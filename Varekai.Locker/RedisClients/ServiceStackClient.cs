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

        IRedisNativeClient _client;

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

            return await ExecCommand(
                lockId.GetSetCommand,
                res => res != null && res.Equals("OK", StringComparison.InvariantCultureIgnoreCase));
        }

        public async Task<string> Confirm(LockId lockId)
        {
            if (!IsConnected(_client))
                await TryConnect();
            
            return await ExecCommand(
                lockId.GetConfirmCommand,
                res => res != null && res.Equals("1"));
        }

        public async Task<string> Release(LockId lockId)
        {
            if (!IsConnected(_client))
                await TryConnect();

            _logger.ToDebugLog(string.Format("Trying to release the lock on {0}:{1}...", _node.Host, _node.Port));

            return await ExecCommand(
                lockId.GetReleaseCommand,
                res => res != null && res.Equals("1"));
        }

        Task<string> ExecCommand(
            Func<object> command,
            Func<string, bool> testResultCorrectness)
        {
            var result = 
                _client
                .RawCommand(command())
                .ToString();

            return testResultCorrectness(result)
                ? _successResult().FromResult()
                : _failureResult().FromResult();
        }

        public bool IsConnected()
        {
            return IsConnected(_client);
        }

        #endregion

        Task<IRedisNativeClient> ConnectClient()
        {
            _logger.ToDebugLog(string.Format("Connecting to the locking node {0}:{1}...", _node.Host, _node.Port));

            var connection = new RedisNativeClient(_node.Host, (int)_node.Port)
                {
                    ConnectTimeout = (int)_node.ConnectTimeoutMillis,
                    SendTimeout = (int)_node.SyncOperationsTimeoutMillis,
                    ReceiveTimeout = (int)_node.SyncOperationsTimeoutMillis
                };

            _logger.ToDebugLog(string.Format("Connected to the locking node {0}:{1}", _node.Host, _node.Port));

            return connection.FromResult<IRedisNativeClient>();
        }

        static bool IsConnected(IRedisNativeClient client)
        {
            return client != null ;
        }

        public void Dispose()
        {
            if(_client != null)
                _client.Dispose();
        }
    }
}