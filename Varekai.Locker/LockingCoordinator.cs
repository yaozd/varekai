using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Varekai.Locker.RedisClients;
using Varekai.Utils.Logging;

namespace Varekai.Locker
{
    public class LockingCoordinator : IDisposable
    {
        readonly CancellationTokenSource _lockAcquisitionCancellation;

        readonly ILogger _logger;
        readonly ReadOnlyCollection<LockingNode> _nodes;
        readonly Func<DateTime> _timeProvider;

        List<IRedisClient> _redisClients;

        LockingCoordinator(IEnumerable<LockingNode> nodes, Func<DateTime> timeProvider, ILogger logger)
        {
            _logger = logger;
            _nodes = new ReadOnlyCollection<LockingNode>(nodes.ToArray());
            _timeProvider = timeProvider;
            _lockAcquisitionCancellation = new CancellationTokenSource();
        }

        static Func<LockingNode, IRedisClient> WithClientFactory()
        {
            return nd => new StackExchangeClient(nd);
        }

        public static LockingCoordinator CreateNewForNodes(IEnumerable<LockingNode> nodes, Func<DateTime> timeProvider, ILogger logger)
        {
            return new LockingCoordinator(nodes, timeProvider, logger);
        }

        public void ConnectNodes()
        {
            _redisClients = 
                _nodes
                    .Select(WithClientFactory())
                    .ToList();
        }

        public async Task<bool> TryAcquireLock(LockId lockId)
        {
            var startTime = _timeProvider();

            var lockAcquired = await TryAcquireLockOnAllNodes(lockId);

            var finishTime = _timeProvider();

            if (lockAcquired 
                && LockingAlgorithm.IsTimeLeftEnoughToUseTheLock(startTime, finishTime, lockId))
                    return lockAcquired;
            
            await TryReleaseTheLock(lockId);
            return false;
        }

        async Task<bool> TryAcquireLockOnAllNodes(LockId lockId)
        {
            return 
                _redisClients.Count != 0 
                &&
                await TryInParallelOnAllClients(
                    (cli, lId) => cli.Set(lId),
                    lockId,
                    "OK");
        }

        public async Task<bool> TryConfirmTheLock(LockId lockId)
        {
            return await TryInParallelOnAllClients(
                (cli, lId) => cli.Confirm(lId),
                lockId,
                "1");
        }

        public async Task TryReleaseTheLock(LockId lockId)
        {
            if (_redisClients == null
                || _redisClients.Count == 0)
                return;

            await TryInParallelOnAllClients(
                (cli, lId) => cli.Release(lId),
                lockId,
                "1");
        }

        async Task<bool> TryInParallelOnAllClients(Func<IRedisClient, LockId, string> operationOnClient, LockId lockId, string successfulResult)
        {
            var quorum = _nodes.CalculateQuorum();

            var sessions = 
                _redisClients
                    .Select(
                        cli => Task.Run(
                            () => { return TryOnClient(operationOnClient, cli, lockId, successfulResult, _logger); }
                            , _lockAcquisitionCancellation.Token));

            var succeded = await Task.WhenAll(sessions);

            return 
                succeded.Count(res => res)
                >=
                quorum;
        }

        static bool TryOnClient(
            Func<IRedisClient, LockId, string> operationOnClient,
            IRedisClient client,
            LockId lockId,
            string successfulResult,
            ILogger logger)
        {
            try
            {
                var result = operationOnClient(client, lockId);

                return 
                    result != null
                    &&
                    result.Equals(
                        successfulResult,
                        StringComparison.InvariantCultureIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.ToErrorLog(ex);
                return false;
            }
        }

        public long GetConfirmationIntervalMillis(LockId lockId)
        {
            return lockId.CalculateConfirmationIntervalMillis();
        }

        public void Dispose()
        {
            if (!_lockAcquisitionCancellation.IsCancellationRequested)
                _lockAcquisitionCancellation.Cancel();

            if (_redisClients == null)
                return;
            
            if (_redisClients.Any())
            {
                foreach (var client in _redisClients)
                    client.Dispose();

                _redisClients.Clear();
            }
        }
    }
}