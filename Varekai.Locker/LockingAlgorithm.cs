using System;
using System.Collections.Generic;
using System.Linq;

namespace Varekai.Locker
{
    public static class LockingAlgorithm
    {
        public static double CalculateRemainingValidityTime(this LockId lockId, DateTime acquisitionStartTime, DateTime acquisitionEndTime)
        {
            var elapsedMilliseconds = acquisitionEndTime.Subtract(acquisitionStartTime).TotalMilliseconds;

            return elapsedMilliseconds < lockId.ExpirationTimeMillis
                ? lockId.ExpirationTimeMillis - elapsedMilliseconds
                : 0;
        }

        public static int CalculateQuorum(this IEnumerable<LockingNode> nodes)
        {
            var nodesHalf = (double)nodes.Count() / 2;
            
            return nodesHalf > 0
                ? (int)Math.Floor(nodesHalf) + 1
                : 0;
        }

        public static double CalculateValidityTimeSafetyMargin(this LockId lockId)
        {
            return (double)lockId.ExpirationTimeMillis / 100;
        }

        public static double CalculateAcquisitionTimeout(this LockId lockId)
        {
            return (double)lockId.ExpirationTimeMillis / 200;
        }

        public static double CalculateConfirmationIntervalMillis(this LockId lockId)
        {
            return (double)lockId.ExpirationTimeMillis / 3;
        }

        public static bool IsTimeLeftEnoughToUseTheLock(DateTime acquisitionStartTime, DateTime acquisitionEndTime, LockId lockId)
        {
            return
                lockId.CalculateRemainingValidityTime(acquisitionStartTime, acquisitionEndTime) 
                >= 
                (lockId.CalculateConfirmationIntervalMillis() + lockId.CalculateValidityTimeSafetyMargin());
        }
    }
}