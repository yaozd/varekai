using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Varekai.Locker;
using Varekai.Utils.Logging;
using Varekai.Utils;

namespace Varekai.Locking.Adapter
{
    public class LockingServiceExecutor : ILockingServiceExecution
    {
        readonly CancellationTokenSource _globalCancellationSource;

        readonly IEnumerable<LockingNode> _lockingNodes;
        readonly ILogger _logger;
        readonly Func<long> _timeProvider;
        readonly IServiceExecution _serviceExecution;
        readonly LockId _lockId;

        readonly Random _numericGenerator;

        LockingCoordinator _lockingCoordinator;
        
        public LockingServiceExecutor(
            IServiceExecution serviceExecution,
            Func<long> timeProvider,
            ILogger logger,
            IEnumerable<LockingNode> lockingNodes,
            LockId lockId)
        {
            _globalCancellationSource = new CancellationTokenSource();

            _serviceExecution = serviceExecution;
            _timeProvider = timeProvider;
            _logger = logger;
            _lockId = lockId;
            _lockingNodes = lockingNodes;

            _numericGenerator = new Random(Guid.NewGuid().GetHashCode());
        }

        #region ILockingServiceExecution implementation

        public async Task LockedStart()
        {
            var holdingLock = false;
            Task serviceStartingTask;

            while (!_globalCancellationSource.IsCancellationRequested)
            {
                try
                {
                    if(_lockingCoordinator == null)
                        _lockingCoordinator = InitCoordinator(
                            _lockingNodes,
                            _timeProvider,
                            _logger);

                    var confirmationInterval = _lockId.CalculateConfirmationIntervalMillis();

                    holdingLock = await _lockingCoordinator.TryAcquireLock(_lockId);

                    if(holdingLock)
                    {
                        _logger.ToInfoLog(string.Format("DISTRIBUTED LOCK ACQUIRED for {0}", _lockId.Resource));

                        serviceStartingTask = Task.Run(StartServiceWhileHodlingLock);

                        _logger.ToInfoLog(string.Format("Entering lock retaining mode for {0}", _lockId.Resource));

                        while(holdingLock && !serviceStartingTask.IsFaulted && !serviceStartingTask.IsCanceled)
                        {
                            holdingLock = await _lockingCoordinator.TryConfirmTheLock(_lockId);

                            await TaskUtils.SilentlyCanceledDelay((int)confirmationInterval, _globalCancellationSource.Token);
                        }

                        if(!_globalCancellationSource.IsCancellationRequested)
                        {
                            _logger.ToDebugLog("Stopping the service for missed confirmation of a previously acquired lock...");

                            _serviceExecution.Stop();

                            _logger.ToDebugLog("Releasing the lock for missed confirmation of a previously acquired lock...");
                        }
                    }
                    else
                    {
                        _logger.ToDebugLog("Unable to acquire the lock, retrying...");

                        var retryInterval = _lockId.CalculateRetryInterval();

                        await TaskUtils.SilentlyCanceledDelay(
                            _numericGenerator.Next(retryInterval.Item1, retryInterval.Item2),
                            _globalCancellationSource.Token);
                    }
                }
                catch (Exception ex)
                {
                    _logger.ToErrorLog(ex);
                }
                finally
                {
                    if(_lockingCoordinator != null)
                        await _lockingCoordinator.TryReleaseTheLock(_lockId);
                }
            }
        }

        public async Task ReleasedStop()
        {
            try
            {
                _globalCancellationSource.Cancel();

                _logger.ToInfoLog("The service is stopping...");

                _serviceExecution.Stop();

                _logger.ToInfoLog(string.Format("Releasing the lock on {0} before shutting the service down...", _lockId.Resource));

                if(_lockingCoordinator != null)
                    await _lockingCoordinator.TryReleaseTheLock(_lockId);

                _logger.ToInfoLog(string.Format("DISTRIBUTED LOCK RELEASED for {0}", _lockId.Resource));
            }
            catch (Exception ex)
            {
                _logger.ToErrorLog(ex);
            }
        }

        #endregion

        static LockingCoordinator InitCoordinator(
            IEnumerable<LockingNode> nodes,
            Func<long> timeProvider,
            ILogger logger)
        {
            logger.ToInfoLog("Creating the locking nodes...");

            return LockingCoordinator
                .CreateNewForNodes(
                    nodes,
                    timeProvider,
                    logger);
        }

        async Task StartServiceWhileHodlingLock()
        {
            //  this guarantees that, in case of a partition of the locking nodes network, all
            // the other services that still believe they hold the lock, have time to fail in confirming it 
            await Task.Delay(
                (int)_lockId.CalculateConfirmationIntervalMillis(),
                _globalCancellationSource.Token);

            _logger.ToInfoLog("Starting the service");

            await _serviceExecution.Start();
        }



        #region IDisposable implementation

        public async void Dispose()
        {
            try
            {
                if(!_globalCancellationSource.IsCancellationRequested)
                    await ReleasedStop();
                    
                _lockingCoordinator.Dispose();

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