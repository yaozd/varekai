using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Redis;
using System.Threading;
using Varekai.Utils.Logging;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace Varekai.Locker
{
    public class LockingCoordinator : IDisposable
    {
        readonly CancellationTokenSource _lockAcquisitionCancellation;

        readonly ILogger _logger;
        readonly ReadOnlyCollection<LockingNode> _nodes;
        readonly Func<DateTime> _timeProvider;

        List<ConnectionMultiplexer> _redisConnections;

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
            _redisConnections = 
                _nodes
                    .Select(
                        nd => ConnectionMultiplexer.Connect(nd.GetConnectionString()))
                    .ToList();
        }

        public bool TryAcquireLock(LockId lockId)
        {
            var startTime = _timeProvider();

            var lockAcquired = TryAcquireLockOnAllNodes(lockId);

            var finishTime = _timeProvider();

            if (!IsLockStillUsable(startTime, finishTime, lockId))
            {
                ReleaseTheLockOnAllNodes(_redisConnections, lockId, _lockAcquisitionCancellation);
                return false;
            }

            return lockAcquired;
        }

        public bool ConfirmTheLock(LockId lockId)
        {
            var sessions = new List<Task<RedisResult>>();

            foreach (var connection in _redisConnections)
            {   
                sessions.Add(
                    Task.Run(
                        () => { return ReleaseTheLockOnNode(connection, lockId); }
                        , _lockAcquisitionCancellation.Token)
                );
            }

            var confirm = Task.WhenAll<RedisResult>(sessions);

            return confirm.IsCompleted;
        }

        public void ReleaseTheLock(LockId lockId)
        {
            if (_redisConnections == null
                || _redisConnections.Count == 0)
                return;
            
            ReleaseTheLockOnAllNodes(_redisConnections, lockId, _lockAcquisitionCancellation);
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
            if (_redisConnections.Count == 0)
                return false;
            
            var sessions = new List<Task<bool>>();

            foreach (var connection in _redisConnections)
            {   
                sessions.Add(
                    Task.Run(
                        () => { return TryAcquireLockOnNode(connection, lockId); }
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

        static bool ReleaseTheLockOnAllNodes(IEnumerable<ConnectionMultiplexer> connections, LockId lockId, CancellationTokenSource cancellation)
        {
            var sessions = new List<Task<RedisResult>>();

            foreach (var connection in connections)
            {   
                sessions.Add(
                    Task.Run(
                        () => { return ReleaseTheLockOnNode(connection, lockId); }
                        , cancellation.Token)
                );
            }

            var release = Task.WhenAll<RedisResult>(sessions);

            return release.IsCompleted;
        }

        static bool TryAcquireLockOnNode(ConnectionMultiplexer connection, LockId lockId)
        {
            return connection
                .GetDatabase()
                .StringSet(
                    (RedisKey)lockId.Resource,
                    (RedisValue)lockId.SessionId.ToString(),
                    TimeSpan.FromMilliseconds(lockId.ExpirationTimeMillis),
                    When.NotExists
            );
        }

        static RedisResult ConfirmTheLockOnNode(ConnectionMultiplexer connection, LockId lockId)
        {
            return connection
                .GetDatabase()
                .ScriptEvaluate(
                    lockId.GetRefreshScript(),
                    new RedisKey[] { lockId.Resource },
                    new RedisValue[] { lockId.SessionId.ToString(), lockId.ExpirationTimeMillis }
                );
        }

        static RedisResult ReleaseTheLockOnNode(ConnectionMultiplexer connection, LockId lockId)
        {
            return connection
                .GetDatabase()
                .ScriptEvaluate(
                    lockId.GetReleaseScript(),
                    new RedisKey[] { lockId.Resource },
                    new RedisValue[] { lockId.SessionId.ToString() }
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

            if (_redisConnections == null)
                return;
            
            foreach (var connection in _redisConnections.Where(con => con.IsConnected))
            {
                connection.Close(false);
                connection.Dispose();
            }

            if(_redisConnections.Any())
                _redisConnections.Clear();
        }
    }
}