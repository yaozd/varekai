using System;
using System.Threading.Tasks;
using ServiceStack.Redis;
using Varekai.Utils.Logging;

namespace Varekai.Locker.RedisClients
{
    public class ServiceStackClient : IRedisClient
    {
        readonly ILogger _logger;
        readonly Func<string> _successResult;
        readonly Func<string> _failureResult;
        readonly LockingNode _node;

        ServiceStack.Redis.IRedisClient _client;

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
            _client = await ConnectClient(_node);
        }

        public async Task<string> Set(LockId lockId)
        {
            if (!IsConnected(_client))
                await TryConnect();

            _logger.ToDebugLog(string.Format("Trying to set the lock on {0}:{1}...", _node.Host, _node.Port));

            return await ExecCommand(
                lockId.GetSetCommand,
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

        async Task<string> ExecCommand(Func<object[]> command, Func<string, bool> testResultCorrectness)
        {
            var result = _client
                .Custom(command())
                .GetResult();

            return testResultCorrectness(result)
                ? _successResult()
                : _failureResult();
        }

        async Task<string> ExecScript(
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
                ? _successResult()
                : _failureResult();
        }

        public bool IsConnected()
        {
            return IsConnected(_client);
        }

        #endregion

        async Task<ServiceStack.Redis.IRedisClient> ConnectClient(LockingNode node)
        {
            _logger.ToDebugLog(string.Format("Connecting to the locking node {0}:{1}...", node.Host, node.Port));

            var connection = new BasicRedisClientManager(GetConnectionString(node)).GetClient();

            _logger.ToDebugLog(string.Format("Connected to the locking node {0}:{1}", node.Host, node.Port));

            return connection;
        }

        static string GetConnectionString(LockingNode node)
        {
            return string.Format(
                @"redis://{0}:{1}?ConnectTimeout={2}&SendTimeout={3}&ReceiveTimeout={3}&Client={4}",
                node.Host,
                node.Port,
                node.ConnectTimeoutMillis,
                node.SyncOperationsTimeoutMillis,
                node.GetNodeName());
        }

        static bool IsConnected(ServiceStack.Redis.IRedisClient client)
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