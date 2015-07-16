using System;
using StackExchange.Redis;
using Varekai.Utils.Logging;
using System.Threading.Tasks;

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

        public async Task<string> Set(LockId lockId)
        {
            if (!IsConnected(_stackExchangeClient))
                _stackExchangeClient = await ConnectClient(_node);
            
            var database = _stackExchangeClient.GetDatabase();

            var result = database.ScriptEvaluate(
                lockId.GetSetScript(),
                new RedisKey[]{ lockId.Resource },
                new RedisValue[]{ lockId.SessionId.ToString() })
            .ToString();

            return result.Equals("OK")
                ? _successResult()
                : _failureResult();
        }

        public async Task<string> Confirm(LockId lockId)
        {
            if (!IsConnected(_stackExchangeClient))
                _stackExchangeClient = await ConnectClient(_node);
            
            var database = _stackExchangeClient.GetDatabase();

            var result = database.ScriptEvaluate(
                lockId.GetConfirmScript(),
                new RedisKey[]{ lockId.Resource },
                new RedisValue[]{ lockId.SessionId.ToString(), (int)lockId.ExpirationTimeMillis })
            .ToString();

            return result.Equals("1")
                ? _successResult()
                : _failureResult();
        }

        public async Task<string> Release(LockId lockId)
        {
            if (!IsConnected(_stackExchangeClient))
                _stackExchangeClient = await ConnectClient(_node);
            
            var database = _stackExchangeClient.GetDatabase();

            var result = database.ScriptEvaluate(
                lockId.GetReleaseScript(),
                new RedisKey[]{ lockId.Resource },
                new RedisValue[]{ lockId.SessionId.ToString() })
            .ToString();

            return result.Equals("1")
                ? _successResult()
                : _failureResult();
        }

        #endregion

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

        static bool IsConnected(ConnectionMultiplexer connectionMultiplexer)
        {
            return connectionMultiplexer != null && connectionMultiplexer.IsConnected;
        }

        public void Dispose()
        {
            if(_stackExchangeClient != null)
                _stackExchangeClient.Close();

            _stackExchangeClient.Dispose();
        }
    }
}