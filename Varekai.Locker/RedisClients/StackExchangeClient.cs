using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using Varekai.Utils.Logging;

namespace Varekai.Locker.RedisClients
{
    public class StackExchangeClient : IRedisClient
    {
        readonly ILogger _logger;
        readonly Func<string> _successResult;
        readonly Func<string> _failureResult;
        readonly LockingNode _node;

        ConnectionMultiplexer _stackExchangeClient;

        public StackExchangeClient(
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
            _stackExchangeClient = await ConnectClient(_node);
        }

        public async Task<string> Set(LockId lockId)
        {
            if (!IsConnected(_stackExchangeClient))
                await TryConnect();

            _logger.ToDebugLog(
                string.Format(
                    "Trying to set the lock on {0}:{1} to {2}...",
                    _node.Host,
                    _node.Port,
                    lockId.SessionId));

            return ExecResultedSetScript(
                lockId.GetSetScript,
                lockId.GetSetScriptParameters
            );
        }

        public async Task<string> Confirm(LockId lockId)
        {
            if (!IsConnected(_stackExchangeClient))
                await TryConnect();

            return ExecResultedConfirmOrReleaseScript(
                lockId.GetConfirmScript,
                lockId.GetConfirmScriptParameters
            );
        }

        public async Task<string> Release(LockId lockId)
        {
            if (!IsConnected(_stackExchangeClient))
                await TryConnect();

            _logger.ToDebugLog(
                string.Format(
                    "Trying to release the lock on {0}:{1} set to {2}...",
                    _node.Host,
                    _node.Port,
                    lockId.SessionId));

            return ExecResultedConfirmOrReleaseScript(
                lockId.GetReleaseScript,
                lockId.GetReleaseScriptParameters
            );
        }

        public bool IsConnected()
        {
            return IsConnected(_stackExchangeClient);
        }

        #endregion

        string ExecResultedSetScript(
            Func<string> script,
            Func<object> parameters)
        {
            return StackExchangeClientHelper.ExecSetScript(
                () =>_stackExchangeClient.GetDatabase(),
                script,
                parameters,
                _successResult,
                _failureResult
            );
        }

        string ExecResultedConfirmOrReleaseScript(
            Func<string> script,
            Func<object> parameters)
        {
            return StackExchangeClientHelper.ExecReleaseOrConfirmScript(
                () =>_stackExchangeClient.GetDatabase(),
                script,
                parameters,
                _successResult,
                _failureResult
            );
        }

        async Task<ConnectionMultiplexer> ConnectClient(LockingNode node)
        {
            _logger.ToDebugLog(string.Format("Connecting to the locking node {0}:{1}...", node.Host, node.Port));

            var connection = await ConnectionMultiplexer.ConnectAsync(GetConnectionString(node));

            _logger.ToDebugLog(string.Format("Connected to the locking node {0}:{1}", node.Host, node.Port));

            return connection;
        }

        static string GetConnectionString(LockingNode node)
        {
            return string.Format(
                @"{0}:{1},connectTimeout={2},syncTimeout={3},name={4},abortConnect=true,configChannel="",tiebreaker=""",
                node.Host,
                node.Port,
                node.ConnectTimeoutMillis,
                node.SyncOperationsTimeoutMillis,
                node.GetNodeName());
        }

        static bool IsConnected(IConnectionMultiplexer connectionMultiplexer)
        {
            return connectionMultiplexer != null && connectionMultiplexer.IsConnected;
        }

        public void Dispose()
        {
            if (_stackExchangeClient != null)
            {
                _stackExchangeClient.Close();
                _stackExchangeClient.Dispose();
            }
        }
    }
}