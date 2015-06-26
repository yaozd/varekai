using System;

namespace Varekai.Locking.Adapter
{
    public interface IServiceExecution : IDisposable
    {
        void Start();
        void Stop();
    }
}

