using System;
using Varekai.Locker;
using Varekai.Utils.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Varekai.Locking.Adapter
{
    public class LockingServiceExecutor : ILockingServiceExecution
    {
        readonly CancellationTokenSource _globalCancellationSource;
        CancellationTokenSource _lockedCancellationSource;

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
            _globalCancellationSource = new CancellationTokenSource();

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

        public async Task LockedStart()
        {
            while (!_globalCancellationSource.IsCancellationRequested)
            {
                try
                {
                    _logger.ToDebugLog("Connecting to the locking nodes");

                    _locker.ConnectNodes();

                    if(_locker.TryAcquireLock(_lockId))
                    {
                        _logger.ToDebugLog("DISTRIBUTED LCOK ACQUIRED");
                        _logger.ToDebugLog("Entering lock retaining mode");

                        //  this guarantees that, in case of a partition of the locking nodes network, all
                        // the other services that still believe they hold the lock, have time to fail in refreshing it 
                        await Task.Delay((int)_locker.GetRefreshTimeMillis(_lockId), _globalCancellationSource.Token);

                        _logger.ToDebugLog("Starting the service");

                        await _serviceExecution.Start();
                    }

                    await Task.Delay(1000, _globalCancellationSource.Token);
                }
                catch (Exception ex)
                {
                    _logger.ToErrorLog(ex);
                }
            }
        }

        public void ReleasedStop()
        {
            try
            {
                _globalCancellationSource.Cancel();

                _logger.ToDebugLog("Releasing the lock...");

                _locker.ReleaseTheLock(_lockId);

                _logger.ToDebugLog("The service is stopping...");

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
                if(!_globalCancellationSource.IsCancellationRequested)
                    ReleasedStop();
                    
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

