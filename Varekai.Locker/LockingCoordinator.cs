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

        List<IRedisClient> _redisClients;

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
            _redisClients = 
                _nodes
                    .Select(
                        nd => new BasicRedisClientManager(nd.GetServiceStackConnectionString()).GetClient())
                    .ToList();
        }

        public bool TryAcquireLock(LockId lockId)
        {
            var startTime = _timeProvider();

            var lockAcquired = TryAcquireLockOnAllNodes(lockId);

            var finishTime = _timeProvider();

            if (!IsLockStillUsable(startTime, finishTime, lockId))
            {
                ReleaseTheLockOnAllNodes(_redisClients, lockId, _lockAcquisitionCancellation);
                return false;
            }

            return lockAcquired;
        }

        public bool ConfirmTheLock(LockId lockId)
        {
            var sessions = new List<Task<string>>();

            foreach (var connection in _redisClients)
            {   
                sessions.Add(
                    Task.Run(
                        () => { return ReleaseTheLockOnNode(connection, lockId); }
                        , _lockAcquisitionCancellation.Token)
                );
            }

            var confirm = Task.WhenAll<string>(sessions);

            return confirm.IsCompleted;
        }

        public void ReleaseTheLock(LockId lockId)
        {
            if (_redisClients == null
                || _redisClients.Count == 0)
                return;
            
            ReleaseTheLockOnAllNodes(_redisClients, lockId, _lockAcquisitionCancellation);
        }

        public long GetConfirmationIntervalMillis(LockId lockId)
        {
            return lockId.ExpirationTimeMillis / 3;
        }

        bool IsLockStillUsable(DateTime acquisitionStartTime, DateTime acquisitionEndTime, LockId lockId)
        {
            return 
                GetRemainingValidityTime(acquisitionStartTime, acquisitionEndTime, lockId) 
                <= 
                (GetConfirmationIntervalMillis(lockId) + GetValidityTimeSafetyMargin(lockId));
        }

        bool TryAcquireLockOnAllNodes(LockId lockId)
        {
            if (_redisClients.Count == 0)
                return false;
            
            var sessions = new List<Task<bool>>();

            foreach (var client in _redisClients)
            {   
                sessions.Add(
                    Task.Run(
                        () => { return TryAcquireLockOnNode(client, lockId); }
                        , _lockAcquisitionCancellation.Token)
                );
            }

            var quorum = GetQuorum(_nodes);
            var acquired = 0;
            var completed = 0;

            while (acquired < quorum || completed < _nodes.Count())
            {
                var completedTry = Task.WhenAny(sessions);

                if (completedTry.IsCompleted && completedTry.Unwrap().Result)
                    acquired++;

                completed++;
            }

            return acquired >= quorum;
        }

        static bool ReleaseTheLockOnAllNodes(IEnumerable<IRedisClient> connections, LockId lockId, CancellationTokenSource cancellation)
        {
            var sessions = new List<Task<string>>();

            foreach (var connection in connections)
            {   
                sessions.Add(
                    Task.Run(
                        () => { return ReleaseTheLockOnNode(connection, lockId); }
                        , cancellation.Token)
                );
            }

            var release = Task.WhenAll<string>(sessions);

            return release.IsCompleted;
        }

        static bool TryAcquireLockOnNode(IRedisClient client, LockId lockId)
        {
            return client
                .Custom(new object[] {
                    Commands.Set,
                    lockId.Resource,
                    lockId.SessionId,
                    "NX",
                    "PX",
                    lockId.ExpirationTimeMillis
                })
                .GetResult()
                .Equals("OK", StringComparison.InvariantCultureIgnoreCase);
        }

        static string ConfirmTheLockOnNode(IRedisClient client, LockId lockId)
        {
            return client
                .ExecLuaAsString(
                    lockId.GetConfirmScript(),
                    new [] { lockId.Resource },
                    new [] { lockId.SessionId.ToString(), lockId.ExpirationTimeMillis.ToString() }
                );
        }

        static string ReleaseTheLockOnNode(IRedisClient client, LockId lockId)
        {
            return client
                .ExecLuaAsString(
                    lockId.GetReleaseScript(),
                    new [] { lockId.Resource },
                    new [] { lockId.SessionId.ToString() }
                );
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

            if (_redisClients == null)
                return;
            
            foreach (var connection in _redisClients)
            {
                connection.Dispose();
            }

            if(_redisClients.Any())
                _redisClients.Clear();
        }
    }
}