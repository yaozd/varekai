using System;
using System.Threading.Tasks;

namespace Varekai.Locker
{
    public interface IRedisClient : IDisposable
    {
        Task<string> Set(LockId lockId);
        Task<string> Confirm(LockId lockId);
        Task<string> Release(LockId lockId);
    }
}

