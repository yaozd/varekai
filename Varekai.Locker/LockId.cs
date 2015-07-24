using System;
using Varekai.Utils;

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

        public object[] GetSetCommand()
        {
            return new object[]
            {
                "SET",
                Resource,
                SessionId,
                "NX",
                "PX",
                ExpirationTimeMillis
            };
        }

        public object[] GetConfirmCommand()
        {
            return  new object[]
            {
                "EVAL",
                GetConfirmScript(),
                Resource,
                SessionId,
                ExpirationTimeMillis.ToCompleteSeconds()
            };
        }

        public object[] GetReleaseCommand()
        {
            return  new object[]
            {
                "EVAL",
                GetReleaseScript(),
                Resource,
                SessionId
            };
        }

        public string GetSetScript()
        {
            return @"return redis.call(""SET"", @resource, @session, ""NX"", ""PX"", @ttl)";
        }

        public object GetSetScriptParameters()
        {
            return new
            { 
                resource = Resource,
                session = SessionId.ToString(),
                ttl = ExpirationTimeMillis
            };
        }

        public string GetConfirmScript()
        {
            return @"if redis.call(""GET"", @resource) == @session then
                    return redis.call(""EXPIRE"", @resource, @ttl)
                else
                    return 0
                end";
        }

        public object GetConfirmScriptParameters()
        {
            return new
            { 
                resource = Resource,
                session = SessionId.ToString(),
                ttl = ExpirationTimeMillis.ToCompleteSeconds()
            };
        }

        public string GetReleaseScript()
        {
            return @"if redis.call(""GET"", @resource) == @session then
                    return redis.call(""DEL"", @resource)
                else
                    return 0
                end";
        }

        public object GetReleaseScriptParameters()
        {
            return new
            { 
                resource = Resource,
                session = SessionId.ToString()
            };
        }
    }
}