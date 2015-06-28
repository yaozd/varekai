using System;
using System.Threading.Tasks;

namespace Varekai.Locking.Adapter
{
    public interface IServiceExecution : IDisposable
    {
        Task Start();
        void Stop();
    }
}

