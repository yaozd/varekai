using System;
using System.Threading;
using ServiceInfrastructureHelper;
using Varekai.Locker;
using Varekai.Locker.Events;
using Varekai.Utils;
using Varekai.Utils.Logging;

namespace SampleLockingService
{
    public class HelloWorldService : IServiceOperation
    {
        readonly LockingEngine _locker;
        readonly ILogger _logger;

        IDisposable _lockingStreamSubscription;

        CancellationTokenSource _serviceCancellation;
        CancellationTokenSource _activityCancellation;

        public HelloWorldService(LockingEngine locker, ILogger logger)
        {
            _locker = locker;
            _logger = logger;
        }

        public void Start()
        {
            _logger.ToInfoLog("Starting Hello World Varekai service...");

            _serviceCancellation = new CancellationTokenSource();
            
            _lockingStreamSubscription = _locker
                .CreateStream()
                .Subscribe(
                    DispatchEvent,
                    HandleError);

            _logger.ToInfoLog("Hello World Varekai service started");
        }

        public void Stop()
        {
            _logger.ToInfoLog("Stopping Hello World Varekai service...");

            if (_activityCancellation != null)
                _activityCancellation.Cancel();

            _lockingStreamSubscription.Dispose();

            if(_serviceCancellation != null)
                _serviceCancellation.Cancel();
            
            _logger.ToInfoLog("Hello World Varekai service stopped");
        }

        public void Dispose(){}

        void DispatchEvent(object @event)
        {
            _logger.ToDebugLog(string.Format("Dispatching event {0}", @event.GetType().FullName));

            if (@event is LockAcquired) _activityCancellation = StartServiceOperation();
            if (@event is LockHeldLost) StopServiceOperation(_activityCancellation);
            if (@event is LockReleaseStarted) StopServiceOperation(_activityCancellation);
            if (@event is LockReleased)
            {
                if (!_serviceCancellation.IsCancellationRequested)
                {
                    if (_activityCancellation != null) _activityCancellation.Cancel();
                    
                    _lockingStreamSubscription = _locker
                        .CreateStream()
                        .Subscribe(DispatchEvent);;
                }
            }
        }

        void HandleError(Exception exception)
        {
            _logger.ToErrorLog(exception);
            _lockingStreamSubscription.Dispose();
            Stop();
        }

        CancellationTokenSource StartServiceOperation()
        {
            _logger.ToDebugLog("Starting service activity...");

            var cancellation = new CancellationTokenSource();

            while (!_serviceCancellation.IsCancellationRequested && !cancellation.IsCancellationRequested)
            {
                _logger.ToInfoLog("Hello World Varekai service running...");

                TaskUtils.SilentlyCanceledDelaySync(2000, _serviceCancellation.Token);
            }

            _logger.ToInfoLog("Hello World Varekai activity complete");

            return cancellation;
        }

        void StopServiceOperation(CancellationTokenSource activityCancellation)
        {
            _logger.ToDebugLog("Stopping service activity...");

            activityCancellation.Cancel();
            _lockingStreamSubscription.Dispose();
        }
    }
}