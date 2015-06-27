using System;
using Varekai.Locker;
using Varekai.Utils.Logging;
using System.Collections.Generic;

namespace Varekai.Locking.Adapter
{
    public class LockingServiceExecutor : ILockingServiceExecution
    {
        readonly LockingCoordinator _locker;
        readonly ILogger _logger;
        readonly Func<DateTime> _timeProvider;

        readonly IServiceExecution _serviceExecution;

        readonly LockId _lockId;
        
        public LockingServiceExecutor(
            IServiceExecution serviceExecution,
            Func<DateTime> timeProvider,
            ILogger logger,
            IEnumerable<LockingNode> lockingNodes,
            LockId lockId)
        {
            _serviceExecution = serviceExecution;
            _timeProvider = timeProvider;
            _logger = logger;
            _lockId = lockId;

            _locker = LockingCoordinator.CreateNewForNodes(
                lockingNodes,
                _timeProvider,
                _logger);
        }

        #region ILockingServiceExecution implementation

        public void LockedStart()
        {
            try
            {
                _logger.ToDebugLog("Connecting to the locking nodes");

                _locker.ConnectNodes();

                if(_locker.TryAcquireLock(_lockId))
                {
                    _logger.ToDebugLog("DISTRIBUTED LCOK ACQUIRED");
                    _logger.ToDebugLog("Entering lock retaining mode");

                    _logger.ToDebugLog("Starting the service");

                    _serviceExecution.Start();
                }
            }
            catch (Exception ex)
            {
                _logger.ToErrorLog(ex);
            }

        }

        public void ReleasedStop()
        {
            try
            {
                _serviceExecution.Stop();
            }
            catch (Exception ex)
            {
                _logger.ToErrorLog(ex);
            }
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            try
            {
                _locker.Dispose();

                _serviceExecution.Dispose();
            }
            catch (Exception ex)
            {
                _logger.ToErrorLog(ex);
            }
        }

        #endregion
    }
}

