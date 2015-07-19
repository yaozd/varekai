using System;
using System.Threading.Tasks;
using Nhiredis;
using Varekai.Utils.Logging;

namespace Varekai.Locker.RedisClients
{
    public class NhiredisClient : IRedisClient
    {
        readonly ILogger _logger;
        readonly Func<string> _successResult;
        readonly Func<string> _failureResult;
        readonly LockingNode _node;

        RedisClient _nHiredisClient;

        public NhiredisClient(
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
            _nHiredisClient = await ConnectClient(_node);
        }

        public async Task<string> Set(LockId lockId)
        {
            if (!IsConnected(_nHiredisClient))
                await TryConnect();

            _logger.ToDebugLog(string.Format("Trying to set the lock on {0}:{1}...", _node.Host, _node.Port));

            return await ExecCommand(
                lockId.GetSetCommand,
                res => res != null && res.Equals("OK", StringComparison.InvariantCultureIgnoreCase));
        }

        public async Task<string> Confirm(LockId lockId)
        {
            if (!IsConnected(_nHiredisClient))
                await TryConnect();

            return await ExecCommand(
                lockId.GetConfirmCommand,
                res => res != null && res.Equals("1"));
        }

        public async Task<string> Release(LockId lockId)
        {
            if (!IsConnected(_nHiredisClient))
                await TryConnect();

            _logger.ToDebugLog(string.Format("Trying to release the lock on {0}:{1}...", _node.Host, _node.Port));

            return await ExecCommand(
                lockId.GetReleaseCommand,
                res => res != null && res.Equals("1"));
        }

        async Task<string> ExecCommand(Func<object[]> command, Func<string, bool> testResultCorrectness)
        {
            if (!IsConnected(_nHiredisClient))
                await TryConnect();

            _logger.ToDebugLog(string.Format("Trying to release the lock on {0}:{1}...", _node.Host, _node.Port));

            var result = _nHiredisClient
                .RedisCommand<string>(command());

            return testResultCorrectness(result)
                ? _successResult()
                : _failureResult();
        }

        public bool IsConnected()
        {
            return IsConnected(_nHiredisClient);
        }

        #endregion

        async Task<RedisClient> ConnectClient(LockingNode node)
        {
            _logger.ToDebugLog(string.Format("Connecting to the locking node {0}:{1}...", node.Host, node.Port));

            var connection = new RedisClient(node.Host, (int)node.Port, TimeSpan.FromMilliseconds(node.ConnectTimeoutMillis));

            _logger.ToDebugLog(string.Format("Connected to the locking node {0}:{1}", node.Host, node.Port));

            return connection;
        }

        static bool IsConnected(RedisClient nHiredisClient)
        {
            return nHiredisClient != null;
        }

        public void Dispose()
        {
            if(_nHiredisClient != null)
                _nHiredisClient.Dispose();
        }
    }
}