using System;
using System.Threading.Tasks;
using ServiceInfrastructureHelper;
using Varekai.Locker;
using Varekai.Locker.Events;
using Varekai.Utils.Logging;
using System.Threading;

namespace SampleLockingService
{
    public class HelloWorldService : IServiceOperation
    {
        readonly LockingEngine _locker;
        readonly ILogger _logger;

        IDisposable _lockingStream;

        CancellationTokenSource _cancellation;

        public HelloWorldService(LockingEngine locker, ILogger logger)
        {
            _locker = locker;
            _logger = logger;
        }

        public void Start()
        {
            _lockingStream = _locker.LockStream(DispatchEvent);
        }

        public void Stop()
        {
            _logger.ToInfoLog("Stopping Hello World Varekai service...");

            _lockingStream.Dispose();

            _logger.ToInfoLog("Hello World Varekai service stopped");
        }

        public void Dispose()
        {
            if(_lockingStream != null)
                _lockingStream.Dispose();
        }

        void DispatchEvent(object @event)
        {
            if(@event is LockAcquired) StartServiceOperation();
            if(@event is LockHeldLost) StopServiceOperation();
            if(@event is LockReleaseStarted) StopServiceOperation();
            if(@event is LockReleased) StartServiceOperation();
        }

        void StartServiceOperation()
        {
            _cancellation = new CancellationTokenSource();

            while (!_cancellation.IsCancellationRequested)
            {
                _logger.ToInfoLog("Hello World Varekai service running...");

                Task
                    .Delay(2000, _cancellation.Token)
                    .Wait(_cancellation.Token);
            }
        }

        void StopServiceOperation()
        {
            _cancellation.Cancel();
        }
    }
}

