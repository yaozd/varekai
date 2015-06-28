using System;
using Varekai.Locking.Adapter;
using System.Threading.Tasks;
using System.Threading;
using Varekai.Utils.Logging;

namespace SampleLockingService
{
    public class SampleServiceImplementation : IServiceExecution
    {
        readonly CancellationTokenSource _cancellation;
        readonly ILogger _logger;

        public SampleServiceImplementation(ILogger logger)
        {
            _logger = logger;
            _cancellation = new CancellationTokenSource();
        }
        
        #region IServiceExecution implementation

        public async Task Start()
        {
            while (!_cancellation.IsCancellationRequested)
            {
                _logger.ToDebugLog("Varekai sample service running");
                
                await Task.Delay(2000, _cancellation.Token);
            }
        }

        public void Stop()
        {
            _cancellation.Cancel();

            _logger.ToDebugLog("Varekai sample service stopped");
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
        }

        #endregion
    }
}

