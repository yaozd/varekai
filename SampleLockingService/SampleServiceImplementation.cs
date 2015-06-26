using System;
using Varekai.Locking.Adapter;

namespace SampleLockingService
{
    public class SampleServiceImplementation : IServiceExecution
    {
        #region IServiceExecution implementation

        public void Start()
        {
            
        }

        public void Stop()
        {
            
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
        }

        #endregion
    }
}

