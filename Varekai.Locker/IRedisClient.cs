using System;

namespace Varekai.Locker
{
    public interface IRedisClient : IDisposable
    {
        string Set(LockId lockId);
        string Confirm(LockId lockId);
        string Release(LockId lockId);
    }
}

