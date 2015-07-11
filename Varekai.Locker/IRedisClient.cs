
namespace Varekai.Locker
{
    public interface IRedisClient
    {
        string Set(LockId lockId);
        string Confirm(LockId lockId);
        string Release(LockId lockId);
    }
}

