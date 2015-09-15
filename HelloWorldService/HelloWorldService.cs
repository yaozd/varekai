using System.Threading.Tasks;
using Varekai.Locking.Adapter;
using Varekai.Utils.Logging;
using Varekai.Locker;
using System;

namespace SampleLockingService
{
    public class HelloWorldService : IServiceOperation
    {
        readonly LockingEngine _locker;
        readonly ILogger _logger;

        IDisposable _lockingStream;

        public HelloWorldService(LockingEngine locker, ILogger logger)
        {
            _locker = locker;
            _logger = logger;
        }

        #region IServiceExecution implementation

        public void Start()
        {
            _lockingStream = _locker.LockStream(DispatchEvent);
        }

        void DispatchEvent(object @event)
        {
            
        }

        void Acquired()
        {
            while (true)
            {
                _logger.ToInfoLog("Hello World Varekai service running...");

                Task.Delay(2000);
            }
        }

        public void Stop()
        {
            _lockingStream.Dispose();

            _logger.ToInfoLog("Hello World Varekai service stopped");
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            _lockingStream.Dispose();
        }

        #endregion
    }
}

