using System;
using System.Threading.Tasks;

namespace Varekai.Locking.Adapter
{
    public interface IServiceOperation : IDisposable
    {
        Task Start();
        void Stop();
    }
}

