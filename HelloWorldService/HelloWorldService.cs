using System.Threading;
using System.Threading.Tasks;
using Varekai.Locking.Adapter;
using Varekai.Utils.Logging;

namespace SampleLockingService
{
    public class HelloWorldService : IServiceExecution
    {
        readonly ILogger _logger;

        CancellationTokenSource _cancellation;

        public HelloWorldService(ILogger logger)
        {
            _logger = logger;
        }
        
        #region IServiceExecution implementation

        public async Task Start()
        {
            _cancellation = new CancellationTokenSource();

            while (!_cancellation.IsCancellationRequested)
            {
                _logger.ToDebugLog("Hello World Varekai service running...");
                
                await Task.Delay(2000, _cancellation.Token);
            }
        }

        public void Stop()
        {
            _cancellation.Cancel();

            _logger.ToDebugLog("Hello World Varekai service stopped");
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
        }

        #endregion
    }
}

