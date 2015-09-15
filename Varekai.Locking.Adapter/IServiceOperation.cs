using System;

namespace Varekai.Locking.Adapter
{
    public interface IServiceOperation : IDisposable
    {
        void Start();
        void Stop();
    }
}

