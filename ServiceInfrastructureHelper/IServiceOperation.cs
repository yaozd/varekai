using System;

namespace ServiceInfrastructureHelper
{
    public interface IServiceOperation : IDisposable
    {
        void Start();
        void Stop();
    }
}

