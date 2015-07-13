using System;
using StackExchange.Redis;

namespace Varekai.Locker.RedisClients
{
    public class StackExchangeClient : IRedisClient
    {
        readonly Func<string> _successResult;
        readonly Func<string> _failureResult;
        readonly ConnectionMultiplexer _stackExchangeClient;

        public StackExchangeClient(
            LockingNode node,
            Func<string> successResult,
            Func<string> failureResult)
        {
            _successResult = successResult;
            _failureResult = failureResult;
            
            _stackExchangeClient = 
                ConnectionMultiplexer.Connect(
                    node.GetStackExchangeConnectionString());
        }

        #region IRedisClient implementation

        public string Set(LockId lockId)
        {
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

        public string Confirm(LockId lockId)
        {
            var database = _stackExchangeClient.GetDatabase();

            var result = database.ScriptEvaluate(
                lockId.GetConfirmScript(),
                new RedisKey[]{ lockId.Resource },
                new RedisValue[]{ lockId.SessionId.ToString(), lockId.ExpirationTimeMillis })
            .ToString();

            return result.Equals("1")
                ? _successResult()
                : _failureResult();
        }

        public string Release(LockId lockId)
        {
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

        public void Dispose()
        {
            if(_stackExchangeClient != null)
                _stackExchangeClient.Close();

            _stackExchangeClient.Dispose();
        }
    }
}