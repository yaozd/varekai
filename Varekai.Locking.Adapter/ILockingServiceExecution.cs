using System;

namespace Varekai.Locking.Adapter
{
    public interface ILockingServiceExecution : IDisposable
    {
        void LockedStart();
        void ReleasedStop();
    }
}

