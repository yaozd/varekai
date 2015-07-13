using System;
using System.Collections.Generic;
using System.Linq;

namespace Varekai.Locker
{
    public static class LockingAlgorithm
    {
        public static double CalculateRemainingValidityTime(this LockId lockId, DateTime acquisitionStartTime, DateTime acquisitionEndTime)
        {
            return lockId.ExpirationTimeMillis - acquisitionEndTime.Subtract(acquisitionStartTime).TotalMilliseconds;
        }

        public static int CalculateQuorum(this IEnumerable<LockingNode> nodes)
        {
            var nodesHalf = (double)nodes.Count() / 2;
            
            return nodesHalf > 0
                ? (int)Math.Floor(nodesHalf) + 1
                : 0;
        }

        public static long CalculateValidityTimeSafetyMargin(this LockId lockId)
        {
            return lockId.ExpirationTimeMillis / 100;
        }

        public static long CalculateAcquisitionTimeout(this LockId lockId)
        {
            return lockId.ExpirationTimeMillis / 200;
        }

        public static long CalculateConfirmationIntervalMillis(this LockId lockId)
        {
            return lockId.ExpirationTimeMillis / 3;
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