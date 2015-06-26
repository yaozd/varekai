using System;
using Varekai.Locker;
using Varekai.Utils.Logging;

namespace Varekai.Locking.Adapter
{
    public class LockingServiceExecutor : ILockingServiceExecution
    {
        readonly LockingCoordinator _locker;
        readonly ILogger _logger;
        readonly Func<DateTime> _timeProvider;

        readonly IServiceExecution _serviceExecution;
        
        public LockingServiceExecutor(
            IServiceExecution serviceExecution,
            Func<DateTime> timeProvider,
            ILogger logger)
        {
            _serviceExecution = serviceExecution;
            _timeProvider = timeProvider;
            _logger = logger;

            //_locker = LockingCoordinator.CreateNewForNodes();
        }

        #region ILockingServiceExecution implementation

        public void LockedStart()
        {
            _serviceExecution.Start();
        }

        public void ReleasedStop()
        {
            _serviceExecution.Stop();
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            _serviceExecution.Dispose();
        }

        #endregion
    }
}

