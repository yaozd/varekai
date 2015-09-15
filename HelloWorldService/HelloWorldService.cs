using System.Threading;
using System.Threading.Tasks;
using Varekai.Locking.Adapter;
using Varekai.Utils.Logging;
using Varekai.Locker;

namespace SampleLockingService
{
    public class HelloWorldService : IServiceOperation
    {
        readonly ILogger _logger;

        public HelloWorldService(ILogger logger)
        {
            _logger = logger;
        }
        
        #region IServiceExecution implementation

        public void Start()
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
            _logger.ToInfoLog("Hello World Varekai service stopped");
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
        }

        #endregion
    }
}

