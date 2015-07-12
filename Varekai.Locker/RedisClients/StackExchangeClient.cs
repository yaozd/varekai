using StackExchange.Redis;

namespace Varekai.Locker.RedisClients
{
    public class StackExchangeClient : IRedisClient
    {
        readonly ConnectionMultiplexer _stackExchangeClient;

        public StackExchangeClient(LockingNode node)
        {
            _stackExchangeClient = 
                ConnectionMultiplexer
                    .Connect(
                        node.GetStackExchangeConnectionString());
        }

        #region IRedisClient implementation

        public string Set(LockId lockId)
        {
            var database = _stackExchangeClient.GetDatabase();

            return database.ScriptEvaluate(
                lockId.GetSetScript(),
                new RedisKey[]{ lockId.Resource },
                new RedisValue[]{ lockId.SessionId.ToString() })
            .ToString();
        }

        public string Confirm(LockId lockId)
        {
            var database = _stackExchangeClient.GetDatabase();

            return database.ScriptEvaluate(
                lockId.GetConfirmScript(),
                new RedisKey[]{ lockId.Resource },
                new RedisValue[]{ lockId.SessionId.ToString(), lockId.ExpirationTimeMillis })
            .ToString();
        }

        public string Release(LockId lockId)
        {
            var database = _stackExchangeClient.GetDatabase();

            return database.ScriptEvaluate(
                lockId.GetReleaseScript(),
                new RedisKey[]{ lockId.Resource },
                new RedisValue[]{ lockId.SessionId.ToString() })
            .ToString();
        }

        #endregion
    }
}

