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
        Tuple<Task, CancellationTokenSource> _sericeActivity;

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

            if(_serviceCancellation != null)
                _serviceCancellation.Cancel();
            
            _logger.ToInfoLog("Hello World Varekai service stopped");
        }

        public void Dispose(){}

        void DispatchEvent(object @event)
        {
            _logger.ToDebugLog(string.Format("Dispatching event {0}", @event.GetType().FullName));

            if (@event is LockAcquired) _sericeActivity = StartServiceOperation();
            if (@event is LockHeldLost) StopServiceOperation(_sericeActivity);
            if (@event is LockReleaseStarted) StopServiceOperation(_sericeActivity);
            if (@event is LockReleased)
            {
                if (!_serviceCancellation.IsCancellationRequested)
                {
                    if (_sericeActivity != null)
                        _sericeActivity.Item2.Cancel();
                    
                    Start();
                }
            }
        }

        Tuple<Task, CancellationTokenSource> StartServiceOperation()
        {
            _logger.ToDebugLog("Starting srvice activity...");

            var cancellation = new CancellationTokenSource();

            return Tuple.Create(
                Task.Run(() =>
                    {
                        while (!_serviceCancellation.IsCancellationRequested && !cancellation.IsCancellationRequested)
                        {
                            _logger.ToInfoLog("Hello World Varekai service running...");

                            TaskUtils.SilentlyCanceledDelaySync(2000, _serviceCancellation.Token);
                        }

                        _logger.ToInfoLog("Hello World Varekai activity complete");
                    })
                , cancellation);
        }

        void StopServiceOperation(Tuple<Task, CancellationTokenSource> serviceActivity)
        {
            _logger.ToDebugLog("Stopping service activity...");

            serviceActivity.Item2.Cancel();

            serviceActivity.Item1.Wait(1000, _serviceCancellation.Token);
        }
    }
}