using System;
using System.Threading.Tasks;

namespace Varekai.Locking.Adapter
{
    public interface ILockingServiceExecution : IDisposable
    {
        Task LockedStart();
        void ReleasedStop();
    }
}

