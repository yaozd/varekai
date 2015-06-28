using System;

namespace Varekai.Locker
{
    public struct LockId
    {
        const long DEFAULT_EXPIRATION_TIME_MILLIS = 5000;
        
        public readonly string Resource;
        public readonly Guid SessionId;
        public readonly long ExpirationTimeMillis;

        LockId(string resource, Guid sessionId, long expirationTimeMillis)
            : this()
        {
            Resource = resource;
            SessionId = sessionId;
            ExpirationTimeMillis = expirationTimeMillis;
        }

        public static LockId CreateNewFor(string resource)
        {
            return new LockId(resource, Guid.NewGuid(), DEFAULT_EXPIRATION_TIME_MILLIS);
        }

        public static LockId CreateNew(string resource, Guid sessionId, long expirationTimeMillis)
        {
            return new LockId(resource, sessionId, expirationTimeMillis);
        }

        public LockId ChangeSessionId(Guid newSessionId)
        {
            return new LockId(Resource, newSessionId, ExpirationTimeMillis);
        }

        public string GetSetCommandText()
        {
            return string.Format(@"SET {0} {1} NX PX {2}", Resource, SessionId, ExpirationTimeMillis);
        }

        public string GetConfirmScript()
        {
            return 
                @"if redis.call(""GET"",KEYS[1]) == ARGV[1] then
                    return redis.call(""EXPIRE"",KEYS[1],ARGV[2])
                else
                    return 0
                end";
        }

        public string GetReleaseScript()
        {
            return 
                @"if redis.call(""GET"",KEYS[1]) == ARGV[1] then
                    return redis.call(""DEL"",KEYS[1])
                else
                    return 0
                end";
        }
    }
}