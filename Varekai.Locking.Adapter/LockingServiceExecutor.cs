﻿using System;
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
        readonly Func<DateTime> _timeProvider;
        readonly IServiceExecution _serviceExecution;
        readonly LockId _lockId;

        LockingCoordinator _locker;
        
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
            _lockingNodes = lockingNodes;
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
                    if(_locker == null)
                    {
                        _logger.ToDebugLog("Creating the locking nodes...");

                        _locker = LockingCoordinator
                            .CreateNewForNodes(
                                _lockingNodes,
                                _timeProvider,
                                _logger);
                    }

                    var confirmationInterval = _locker.GetConfirmationIntervalMillis(_lockId);

                    holdingLock = await _locker.TryAcquireLock(_lockId);

                    if(holdingLock)
                    {
                        serviceStartingTask = Task.Run(StartServiceWhileHodlingLock);

                        while(holdingLock && !serviceStartingTask.IsFaulted && !serviceStartingTask.IsCanceled)
                        {
                            holdingLock = await _locker.TryConfirmTheLock(_lockId);

                            await TaskUtils.SilentlyCanceledDelay((int)confirmationInterval, _globalCancellationSource.Token);
                        }

                        _logger.ToDebugLog("Stopping the service for missed confirmation of a previously acquired lock...");

                        _serviceExecution.Stop();

                        _logger.ToDebugLog("Releasing the lock for missed confirmation of a previously acquired lock...");
                    }
                    else
                    {
                        _logger.ToDebugLog("Unable to acquire the lock, retrying...");

                        // TODO: add random interval to the retry
                        await TaskUtils.SilentlyCanceledDelay(1000, _globalCancellationSource.Token);
                    }
                }
                catch (Exception ex)
                {
                    _logger.ToErrorLog(ex);
                }
                finally
                {
                    if(_locker != null)
                        await _locker.TryReleaseTheLock(_lockId);
                }
            }
        }

        public async Task ReleasedStop()
        {
            try
            {
                _globalCancellationSource.Cancel();

                _logger.ToDebugLog("Releasing the lock before shutting the service down...");

                if(_locker != null)
                    await _locker.TryReleaseTheLock(_lockId);

                _logger.ToDebugLog("The service is stopping...");

                _serviceExecution.Stop();
            }
            catch (Exception ex)
            {
                _logger.ToErrorLog(ex);
            }
        }

        #endregion

        async Task StartServiceWhileHodlingLock()
        {
            _logger.ToDebugLog("DISTRIBUTED LCOK ACQUIRED");
            _logger.ToDebugLog("Entering lock retaining mode");

            //  this guarantees that, in case of a partition of the locking nodes network, all
            // the other services that still believe they hold the lock, have time to fail in confirming it 
            await Task.Delay(
                (int)_locker.GetConfirmationIntervalMillis(_lockId),
                _globalCancellationSource.Token);

            _logger.ToDebugLog("Starting the service");

            await _serviceExecution.Start();
        }

        #region IDisposable implementation

        public async void Dispose()
        {
            try
            {
                if(!_globalCancellationSource.IsCancellationRequested)
                    await ReleasedStop();
                    
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