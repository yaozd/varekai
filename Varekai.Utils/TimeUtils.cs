using System;
using System.Diagnostics;

namespace Varekai.Utils
{
    public static class TimeUtils
    {
        public static long ToCompleteSeconds(this long milliseconds)
        {
            return milliseconds
                .CastFromTo<long, double>()
                .ToSeconds()
                .CastFromTo<double, long>();
        }

        public static double ToSeconds(this double milliseconds)
        {
            return milliseconds
                .WithMillisecondsCreateTimeSpan()
                .TotalSeconds;
        }

        /// <summary>
        /// Creates a timespan from the double milliseconds without truncating the decimals 
        /// </summary>
        public static TimeSpan WithMillisecondsCreateTimeSpan(this double milliseconds)
        {
            return TimeSpan.FromTicks(
                (long)
                (milliseconds * TimeSpan.TicksPerMillisecond)
            );
        }

        public static Func<long> MonotonicTimeTicksProvider()
        {
            return () => Stopwatch.GetTimestamp();
        }
    }
}

