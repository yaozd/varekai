using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Redis;
using Varekai.Utils.Logging;

namespace Varekai.Locker
{
    public class LockingCoordinator : IDisposable
    {
        readonly CancellationTokenSource _lockAcquisitionCancellation;

        readonly ILogger _logger;
        readonly ReadOnlyCollection<LockingNode> _nodes;
        readonly Func<DateTime> _timeProvider;

        List<BasicRedisClientManager> _redisClientManagers;

        LockingCoordinator(IEnumerable<LockingNode> nodes, Func<DateTime> timeProvider, ILogger logger)
        {
            _logger = logger;
            _nodes = new ReadOnlyCollection<LockingNode>(nodes.ToArray());
            _timeProvider = timeProvider;
            _lockAcquisitionCancellation = new CancellationTokenSource();
        }

        public static LockingCoordinator CreateNewForNodes(IEnumerable<LockingNode> nodes, Func<DateTime> timeProvider, ILogger logger)
        {
            return new LockingCoordinator(nodes, timeProvider, logger);
        }

        public void ConnectNodes()
        {
            _redisClientManagers = 
                _nodes
                    .Select(
                        nd => new BasicRedisClientManager(nd.GetServiceStackConnectionString()))
                    .ToList();
        }

        public async Task<bool> TryAcquireLock(LockId lockId)
        {
            var startTime = _timeProvider();

            var lockAcquired = await TryAcquireLockOnAllNodes(lockId);

            var finishTime = _timeProvider();

            if (!IsLockStillUsable(startTime, finishTime, lockId))
            {
                ReleaseTheLockOnAllNodes(_redisClientManagers, lockId, _lockAcquisitionCancellation, _logger);
                return false;
            }

            return lockAcquired;
        }

        public bool ConfirmTheLock(LockId lockId)
        {
            var sessions = new List<Task<string>>();

            foreach (var manager in _redisClientManagers)
            {   
                sessions.Add(
                    Task.Run(
                        () => { return ReleaseTheLockOnNode(manager, lockId, _logger); }
                        , _lockAcquisitionCancellation.Token)
                );
            }

            var confirm = Task.WhenAll<string>(sessions);

            return confirm.IsCompleted;
        }

        public void ReleaseTheLock(LockId lockId)
        {
            if (_redisClientManagers == null
                || _redisClientManagers.Count == 0)
                return;
            
            ReleaseTheLockOnAllNodes(_redisClientManagers, lockId, _lockAcquisitionCancellation, _logger);
        }

        public long GetConfirmationIntervalMillis(LockId lockId)
        {
            return lockId.ExpirationTimeMillis / 3;
        }

        bool IsLockStillUsable(DateTime acquisitionStartTime, DateTime acquisitionEndTime, LockId lockId)
        {
            return
                GetRemainingValidityTime(acquisitionStartTime, acquisitionEndTime, lockId) 
                >= 
                (GetConfirmationIntervalMillis(lockId) + GetValidityTimeSafetyMargin(lockId));
        }

        async Task<bool> TryAcquireLockOnAllNodes(LockId lockId)
        {
            if (_redisClientManagers.Count == 0)
                return false;
            
            var quorum = GetQuorum(_nodes);
            var sessions = new List<Task<bool>>();

            sessions.AddRange(
                _redisClientManagers
                .Select(
                    cli => Task.Run(
                        () => { return TryAcquireLockOnNode(cli, lockId, _logger); }
                        , _lockAcquisitionCancellation.Token))
                .ToArray());
            
            var acquired = 0;
            var completed = 0;

            while (acquired < quorum && completed < _nodes.Count())
            {
                var completedTry = await Task.WhenAny(sessions);

                if (completedTry.IsCompleted && completedTry.Result)
                    acquired++;

                completed++;
            }

            return acquired >= quorum;
        }

        static bool ReleaseTheLockOnAllNodes(
            IEnumerable<BasicRedisClientManager> connections,
            LockId lockId,
            CancellationTokenSource cancellation,
            ILogger logger)
        {
            var sessions = new List<Task<string>>();

            foreach (var connection in connections)
            {   
                sessions.Add(
                    Task.Run(
                        () => { return ReleaseTheLockOnNode(connection, lockId, logger); }
                        , cancellation.Token)
                );
            }

            var release = Task.WhenAll<string>(sessions);

            return release.IsCompleted;
        }

        static bool TryAcquireLockOnNode(IRedisClientsManager clientManager, LockId lockId, ILogger logger)
        {
            try
            {
                using (var client = clientManager.GetClient())
                {
                    var result = client
                        .Custom(new object[] {
                            Commands.Set,
                            lockId.Resource,
                            lockId.SessionId,
                            "NX",
                            "PX",
                            lockId.ExpirationTimeMillis
                        })
                        .GetResult();
                    
                    return 
                        result
                        .Equals("OK", StringComparison.InvariantCultureIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                logger.ToErrorLog(ex);
                return false;
            }
        }

        static string ConfirmTheLockOnNode(IRedisClientsManager clientManager, LockId lockId, ILogger logger)
        {
            try
            {
                using (var client = clientManager.GetClient())
                {
                    return client
                        .ExecLuaAsString(
                            lockId.GetConfirmScript(),
                            new [] { lockId.Resource },
                            new [] { lockId.SessionId.ToString(), lockId.ExpirationTimeMillis.ToString() }
                        );
                }
            }
            catch (Exception ex)
            {
                logger.ToErrorLog(ex);
                return ex.Message;
            }
        }

        static string ReleaseTheLockOnNode(IRedisClientsManager clientManager, LockId lockId, ILogger logger)
        {
            try
            {
                using (var client = clientManager.GetClient())
                {
                    return client
                        .ExecLuaAsString(
                            lockId.GetReleaseScript(),
                            new [] { lockId.Resource },
                            new [] { lockId.SessionId.ToString() }
                        );
                }
            }
            catch (Exception ex)
            {
                logger.ToErrorLog(ex);
                return ex.Message;
            }
        }

        static double GetRemainingValidityTime(DateTime acquisitionStartTime, DateTime acquisitionEndTime, LockId lockId)
        {
            return lockId.ExpirationTimeMillis - acquisitionEndTime.Subtract(acquisitionStartTime).TotalMilliseconds;
        }

        static int GetQuorum(IEnumerable<LockingNode> nodes)
        {
            return (int)(Math.Floor((double)nodes.Count() / 2)) + 1;
        }

        static long GetValidityTimeSafetyMargin(LockId lockId)
        {
            return lockId.ExpirationTimeMillis / 100;
        }

        static long GetAcquisitionTimeout(LockId lockId)
        {
            return lockId.ExpirationTimeMillis / 200;
        }

        public void Dispose()
        {
            if (!_lockAcquisitionCancellation.IsCancellationRequested)
                _lockAcquisitionCancellation.Cancel();

            if (_redisClientManagers == null)
                return;
            
            if(_redisClientManagers.Any())
                _redisClientManagers.Clear();
        }
    }
}