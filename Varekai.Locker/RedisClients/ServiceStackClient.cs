using System;
using System.Threading.Tasks;
using ServiceStack.Redis;
using Varekai.Utils;
using Varekai.Utils.Logging;

namespace Varekai.Locker.RedisClients
{
    public class ServiceStackClient : IRedisClient
    {
        readonly ILogger _logger;
        readonly Func<string> _successResult;
        readonly Func<string> _failureResult;
        readonly LockingNode _node;

        RedisClient _client;

        public ServiceStackClient(
            LockingNode node,
            Func<string> successResult,
            Func<string> failureResult,
            ILogger logger)
        {
            _logger = logger;
            _successResult = successResult;
            _failureResult = failureResult;
            _node = node;
        }

        #region IRedisClient implementation

        public async Task TryConnect()
        {
            _client = await ConnectClient();
        }

        public async Task<string> Set(LockId lockId)
        {
            if (!IsConnected(_client))
                await TryConnect();

            _logger.ToDebugLog(string.Format("Trying to set the lock on {0}:{1}...", _node.Host, _node.Port));

            return await ExecScript(
                lockId.GetSetScript,
                () => new [] { lockId.Resource },
                () => new [] { lockId.SessionId.ToString(), lockId.ExpirationTimeMillis.ToString() },
                res => res != null && res.Equals("OK", StringComparison.InvariantCultureIgnoreCase));
        }

        public async Task<string> Confirm(LockId lockId)
        {
            if (!IsConnected(_client))
                await TryConnect();
            
            return await ExecScript(
                lockId.GetConfirmScript,
                () => new [] { lockId.Resource },
                () => new [] { lockId.SessionId.ToString(), lockId.ExpirationTimeMillis.ToString() },
                res => res != null && res.Equals("1"));
        }

        public async Task<string> Release(LockId lockId)
        {
            if (!IsConnected(_client))
                await TryConnect();

            _logger.ToDebugLog(string.Format("Trying to release the lock on {0}:{1}...", _node.Host, _node.Port));

            return await ExecScript(
                lockId.GetReleaseScript,
                () => new [] { lockId.Resource },
                () => new [] { lockId.SessionId.ToString() },
                res => res != null && res.Equals("1"));
        }

        Task<string> ExecScript(
            Func<string> script,
            Func<string[]> keys,
            Func<string[]> args,
            Func<string, bool> testResultCorrectness)
        {
            var result = _client
                .ExecLuaAsString(
                    script(),
                    keys(),
                    args());

            return testResultCorrectness(result)
                ? _successResult().FromResult()
                : _failureResult().FromResult();
        }

        public bool IsConnected()
        {
            return IsConnected(_client);
        }

        #endregion

        Task<RedisClient> ConnectClient()
        {
            _logger.ToDebugLog(string.Format("Connecting to the locking node {0}:{1}...", _node.Host, _node.Port));

            var connection = new RedisClient(_node.Host, (int)_node.Port)
                {
                    ConnectTimeout = (int)_node.ConnectTimeoutMillis,
                    SendTimeout = (int)_node.SyncOperationsTimeoutMillis,
                    ReceiveTimeout = (int)_node.SyncOperationsTimeoutMillis
                };

            _logger.ToDebugLog(string.Format("Connected to the locking node {0}:{1}", _node.Host, _node.Port));

            return connection.FromResult();
        }

        static bool IsConnected(RedisClient client)
        {
            return client != null;
        }

        public void Dispose()
        {
            if(_client != null)
                _client.Dispose();
        }
    }
}