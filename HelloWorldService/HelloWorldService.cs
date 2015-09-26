using System;
using System.Threading;
using System.Threading.Tasks;
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
        CancellationTokenSource _helloWordlActivityCancellation;

        public HelloWorldService(LockingEngine locker, ILogger logger)
        {
            _locker = locker;
            _logger = logger;
        }

        public void Start()
        {
            _logger.ToInfoLog("Starting Hello World Varekai service...");

            if(_serviceCancellation == null)
                _serviceCancellation = new CancellationTokenSource();
            
            _lockingStreamSubscription = _locker
                .CreateStream()
                .Subscribe(DispatchEvent);

            _logger.ToInfoLog("Hello World Varekai service started");
        }

        public void Stop()
        {
            _logger.ToInfoLog("Stopping Hello World Varekai service...");

            _lockingStreamSubscription.Dispose();

            _logger.ToInfoLog("Hello World Varekai service stopped");
        }

        public void Dispose()
        {
            if(_serviceCancellation != null)
                _serviceCancellation.Cancel();

            if (_helloWordlActivityCancellation != null)
                _helloWordlActivityCancellation.Cancel();

            if(_lockingStreamSubscription != null)
                _lockingStreamSubscription.Dispose();
        }

        void DispatchEvent(object @event)
        {
            _logger.ToDebugLog(string.Format("Dispatching event {0}", @event.GetType().FullName));

            if (@event is LockAcquired) StartServiceOperation();
            if (@event is LockHeldLost) StopServiceOperation();
            if (@event is LockReleaseStarted) StopServiceOperation();
            if (@event is LockReleased)
            {
                if (!_serviceCancellation.IsCancellationRequested)
                {
                    _lockingStreamSubscription.Dispose();
                    _lockingStreamSubscription = _locker
                        .CreateStream()
                        .Subscribe(DispatchEvent);
                }
            }
        }

        Task StartServiceOperation()
        {
            _logger.ToDebugLog("Starting srvice activity...");

            if(_helloWordlActivityCancellation == null)
                _helloWordlActivityCancellation = new CancellationTokenSource();

            return Task.Run(() =>
                {
                    while (!_serviceCancellation.IsCancellationRequested && !_helloWordlActivityCancellation.IsCancellationRequested)
                    {
                        _logger.ToInfoLog("Hello World Varekai service running...");

                        TaskUtils.SilentlyCanceledDelaySync(2000, _serviceCancellation.Token);
                    }

                    _logger.ToInfoLog("Hello World Varekai activity complete");
                });
        }

        void StopServiceOperation()
        {
            _logger.ToDebugLog("Stopping service activity...");

            if (_helloWordlActivityCancellation != null)
            {
                _helloWordlActivityCancellation.Cancel();
                _helloWordlActivityCancellation = null;
            }
        }
    }
}