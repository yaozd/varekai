using System;
using System.Threading;
using Varekai.Utils.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Varekai.Utils;
using System.Reactive.Linq;

namespace Varekai.Locker
{
    public class LockingEngine
    {
        readonly IEnumerable<LockingNode> _lockingNodes;
        readonly ILogger _logger;
        readonly Func<long> _timeProvider;
        readonly LockId _lockId;

        public IObservable<object> StartLockingSequence()
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
                        .Wait();

                    return null;
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

                        //TODO notify acquired
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
                while(holdingLock)
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

            //TODO Notify acquired lock lost
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

                logger.ToInfoLog("The service is stopping...");

                //TODO releasing lock stop before

                logger.ToInfoLog(string.Format("Releasing the lock on {0} before shutting the service down...", _lockId.Resource));

                if(lockingCoordinator != null)
                    await lockingCoordinator.TryReleaseTheLock(lockId).ConfigureAwait(false);
                
                logger.ToInfoLog(string.Format("DISTRIBUTED LOCK RELEASED for {0}", _lockId.Resource));

                //TODO release complete
            }
            catch (Exception ex)
            {
                logger.ToErrorLog(ex);
            }
        }
    }
}