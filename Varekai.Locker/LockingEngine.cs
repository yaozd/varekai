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

        public LockingEngine(
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

        public IObservable<object> CreateStream()
        {
            return Observable.Create<object>(
                async observer => 
                {
                    var cancellation = new CancellationTokenSource();
                    var coordinator = await InitCoordinator(_lockingNodes, _timeProvider, _logger).ConfigureAwait(false);
                     
                    await StartAttemptingAcquisition(coordinator, _lockId, _logger, cancellation, observer).ConfigureAwait(false);
                
                    await KeepConfirmingTheLock(coordinator, _logger, cancellation, observer).ConfigureAwait(false);

                    await ReleaseLock(coordinator, _lockId, _logger, cancellation, observer).ConfigureAwait(false);

                    coordinator.Dispose();

                    observer.OnCompleted();

                    return async () => 
                    {
                        _logger.ToInfoLog("Disposing the locking stream...");

                        if(cancellation != null && !cancellation.IsCancellationRequested)
                            cancellation.Cancel();
                    };
                })
                .SubscribeOn(ThreadPoolScheduler.Instance);
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
                    observer.OnError(ex);

                    if(lockingCoordinator != null)
                        await lockingCoordinator
                            .TryReleaseTheLock(lockId)
                            .ConfigureAwait(false);

                    holdingLock = false;
                }
            }
        }

        async Task KeepConfirmingTheLock(
            LockingCoordinator lockingCoordinator,
            ILogger logger,
            CancellationTokenSource lockingCancellationSource,
            IObserver<object> observer)
        {
            if (lockingCoordinator == null)
            {
                observer.OnError(new ApplicationException("Trying to confirm lock on a NULL coordinator. Invalid locking engine state. Aborting..."));

                return;
            }

            var acquisitionSignaled = false;
            var holdingLock = true;
            var confirmationInterval = _lockId.CalculateConfirmationIntervalMillis();

            logger.ToInfoLog(string.Format("Entering lock retaining mode for {0}", _lockId.Resource));

            try
            {
                while(!lockingCancellationSource.IsCancellationRequested && holdingLock)
                {
                    await TaskUtils
                        .SilentlyCanceledDelay(
                            (int)confirmationInterval,
                            lockingCancellationSource.Token)
                        .ConfigureAwait(false);

                    //  in a partition guarantees that the other lock holder have the time to realize
                    //  it's not holding the lock anymore
                    if(!acquisitionSignaled)
                    {
                        observer.OnNext(new LockAcquired());
                        acquisitionSignaled = true;
                    }
                    
                    holdingLock = await lockingCoordinator
                                    .TryConfirmTheLock(_lockId)
                                    .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }

            if(!lockingCancellationSource.IsCancellationRequested)
                logger.ToInfoLog(string.Format("Lock held for {0} lost", _lockId.Resource));

            observer.OnNext(new LockHeldLost());

            if (lockingCoordinator != null)
                await lockingCoordinator
                    .TryReleaseTheLock(_lockId)
                    .ConfigureAwait(false);
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

                logger.ToInfoLog(string.Format("Releasing the lock on {0}...", _lockId.Resource));

                if(lockingCoordinator != null)
                    await lockingCoordinator
                        .TryReleaseTheLock(lockId)
                        .ConfigureAwait(false);
                
                logger.ToInfoLog(string.Format("DISTRIBUTED LOCK RELEASED for {0}", _lockId.Resource));

                observer.OnNext(new LockReleased());
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        }
    }
}