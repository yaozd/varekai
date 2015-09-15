using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Varekai.Locker.Events;
using Varekai.Utils;
using Varekai.Utils.Logging;

namespace Varekai.Locker
{
    public class LockingEngine
    {
        readonly IEnumerable<LockingNode> _lockingNodes;
        readonly ILogger _logger;
        readonly Func<long> _timeProvider;
        readonly LockId _lockId;

        LockingEngine(
            Func<long> timeProvider,
            ILogger logger,
            IEnumerable<LockingNode> lockingNodes,
            LockId lockId)
        {
            _timeProvider = timeProvider;
            _logger = logger;
            _lockId = lockId;
            _lockingNodes = lockingNodes;
        }

        public static IObservable<object> Acquire(
            Func<long> timeProvider,
            ILogger logger,
            IEnumerable<LockingNode> lockingNodes,
            LockId lockId)
        {
            var lockingEngine = new LockingEngine(timeProvider, logger, lockingNodes, lockId);

            return
                lockingEngine
                .StartLockingProcess()
                .SubscribeOn(ThreadPoolScheduler.Instance)
                .ObserveOn(Scheduler.CurrentThread);
        }

        IObservable<object> StartLockingProcess()
        {
            return Observable.Create<object>(
                observer => 
                {
                    var cancellation = new CancellationTokenSource();
                    var coordinator = InitCoordinator(_lockingNodes, _timeProvider, _logger).Result;

                    StartAttemptingAcquisition(coordinator, _lockId, _logger, cancellation, observer)
                        .Wait(cancellation.Token);

                    KeepConfirmingLock(coordinator, _logger, cancellation, observer)
                        .Wait(cancellation.Token);

                    ReleaseLock(coordinator,_lockId, _logger, cancellation, observer)
                        .Wait(cancellation.Token);

                    coordinator.Dispose();

                    return () => 
                    {
                        if(cancellation != null && !cancellation.IsCancellationRequested)
                            cancellation.Cancel();
                    };
                });
        }

        async static Task<LockingCoordinator> InitCoordinator(
            IEnumerable<LockingNode> lockingNodes,
            Func<long> timeProvider,
            ILogger logger)
        {
            logger.ToInfoLog("Creating the locking nodes...");

            return await LockingCoordinator
                .CreateNewForNodes(lockingNodes, timeProvider, logger)
                .ConfigureAwait(false);
        }

        async static Task StartAttemptingAcquisition(
            LockingCoordinator lockingCoordinator,
            LockId lockId,
            ILogger logger,
            CancellationTokenSource lockingCancellationSource,
            IObserver<object> observer)
        {
            var holdingLock = false;
            var retryInterval = lockId.CalculateRetryInterval();
            var randomGenerator = new Random(Guid.NewGuid().GetHashCode());

            while (!lockingCancellationSource.IsCancellationRequested && !holdingLock)
            {
                try
                {
                    holdingLock = await lockingCoordinator.TryAcquireLock(lockId).ConfigureAwait(false);

                    if(holdingLock)
                    {
                        logger.ToInfoLog(string.Format("DISTRIBUTED LOCK ACQUIRED for {0}", lockId.Resource));

                        observer.OnNext(new LockAcquired());
                    }
                    else
                    {
                        logger.ToDebugLog("Unable to acquire the lock, retrying...");

                        await TaskUtils.SilentlyCanceledDelay(
                            randomGenerator.Next(retryInterval.Item1, retryInterval.Item2),
                            lockingCancellationSource.Token)
                        .ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    logger.ToErrorLog(ex);
                }
                finally
                {
                    if(lockingCoordinator != null)
                        await lockingCoordinator.TryReleaseTheLock(lockId).ConfigureAwait(false);
                }
            }
        }

        async Task KeepConfirmingLock(
            LockingCoordinator lockingCoordinator,
            ILogger logger,
            CancellationTokenSource lockingCancellationSource,
            IObserver<object> observer)
        {
            if (lockingCoordinator == null)
            {
                //TODO error, incorrect state

                return;
            }
                
            var holdingLock = true;
            var confirmationInterval = _lockId.CalculateConfirmationIntervalMillis();

            logger.ToInfoLog(string.Format("Entering lock retaining mode for {0}", _lockId.Resource));

            try
            {
                while(!lockingCancellationSource.IsCancellationRequested && holdingLock)
                {
                    holdingLock = await lockingCoordinator
                        .TryConfirmTheLock(_lockId)
                        .ConfigureAwait(false);

                    await TaskUtils
                        .SilentlyCanceledDelay((int)confirmationInterval, lockingCancellationSource.Token)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                logger.ToErrorLog(ex);
            }
            finally
            {
                if (lockingCoordinator != null)
                    await lockingCoordinator.TryReleaseTheLock(_lockId).ConfigureAwait(false);
            }

            observer.OnNext(new LockHeldLost());
        }

        async Task ReleaseLock(
            LockingCoordinator lockingCoordinator,
            LockId lockId,
            ILogger logger,
            CancellationTokenSource lockingCancellationSource,
            IObserver<object> observer)
        {
            try
            {
                if(!lockingCancellationSource.IsCancellationRequested)
                    lockingCancellationSource.Cancel();

                observer.OnNext(new LockReleaseStarted());

                logger.ToInfoLog(string.Format("Releasing the lock on {0} before shutting the service down...", _lockId.Resource));

                if(lockingCoordinator != null)
                    await lockingCoordinator.TryReleaseTheLock(lockId).ConfigureAwait(false);
                
                logger.ToInfoLog(string.Format("DISTRIBUTED LOCK RELEASED for {0}", _lockId.Resource));

                observer.OnNext(new LockReleased());
            }
            catch (Exception ex)
            {
                logger.ToErrorLog(ex);
            }
        }
    }
}