using System;

namespace Varekai.Utils
{
    public static class TimeUtils
    {
        public static long ToCompleteSeconds(this long milliseconds)
        {
            return (long)milliseconds.ToSeconds();
        }

        public static double ToSeconds(this long milliseconds)
        {
            return TimeSpan
                .FromMilliseconds(milliseconds)
                .TotalSeconds;
        }
    }
}

