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
                ReleaseTheLockOnAllNodes(lockId);
                return false;
            }

            return lockAcquired;
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
                    cliManager => Task.Run(
                        () => { return TryAcquireLockOnNode(cliManager, lockId, _logger); }
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

        public async Task<bool> ConfirmTheLock(LockId lockId)
        {
            var sessions = new List<Task<bool>>();
            var quorum = GetQuorum(_nodes);

            sessions.AddRange(
                _redisClientManagers
                .Select(
                    cliManager => Task.Run(
                        () => { return ReleaseTheLockOnNode(cliManager, lockId, _logger); }
                        , _lockAcquisitionCancellation.Token))
                .ToArray());

            var confirm = await Task.WhenAll<bool>(sessions);

            return confirm.Count(val => val) >= quorum;
        }

        public void ReleaseTheLock(LockId lockId)
        {
            if (_redisClientManagers == null
                || _redisClientManagers.Count == 0)
                return;

            ReleaseTheLockOnAllNodes(lockId);
        }

        async Task<bool> ReleaseTheLockOnAllNodes(LockId lockId)
        {
            var sessions = new List<Task<bool>>();
            var quorum = GetQuorum(_nodes);

            sessions.AddRange(
                _redisClientManagers
                .Select(
                    cliManager => Task.Run(
                        () => { return ReleaseTheLockOnNode(cliManager, lockId, _logger); }
                        , _lockAcquisitionCancellation.Token))
                .ToArray());

            var released = await Task.WhenAll<bool>(sessions);

            return released.Count(val => val) >= quorum;
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
                        result != null
                        &&
                        result.Equals("OK", StringComparison.InvariantCultureIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                logger.ToErrorLog(ex);
                return false;
            }
        }

        static bool ConfirmTheLockOnNode(IRedisClientsManager clientManager, LockId lockId, ILogger logger)
        {
            try
            {
                using (var client = clientManager.GetClient())
                {
                    var result =  client
                        .ExecLuaAsString(
                            lockId.GetConfirmScript(),
                            new [] { lockId.Resource },
                            new [] { lockId.SessionId.ToString(), lockId.ExpirationTimeMillis.ToString() }
                        );
                    
                    return 
                        result
                        .Equals("1", StringComparison.InvariantCultureIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                logger.ToErrorLog(ex);
                return false;
            }
        }

        static bool ReleaseTheLockOnNode(IRedisClientsManager clientManager, LockId lockId, ILogger logger)
        {
            try
            {
                using (var client = clientManager.GetClient())
                {
                    var result =  client
                        .ExecLuaAsString(
                            lockId.GetReleaseScript(),
                            new [] { lockId.Resource },
                            new [] { lockId.SessionId.ToString() }
                        );

                    return 
                        result
                        .Equals("1", StringComparison.InvariantCultureIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                logger.ToErrorLog(ex);
                return false;
            }
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