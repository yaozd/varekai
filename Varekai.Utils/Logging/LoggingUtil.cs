using System;

namespace Varekai.Utils.Logging
{
    public static class LoggingUtil
    {
        public static void ToDebugLog(this string message, Func<ILogger> loggingHandle)
        {
            message.EnsureIsNotNull("message");
            loggingHandle.EnsureIsNotNull("loggingHandle");

            loggingHandle().ToDebugLog(message);
        }

        public static void ToErrorLog(this Exception exception, Func<ILogger> loggingHandle)
        {
            exception.EnsureIsNotNull("exception");
            loggingHandle.EnsureIsNotNull("loggingHandle");

            loggingHandle().ToErrorLog(exception);
        }

        public static void ToErrorLog(this AggregateException exception, Func<ILogger> loggingHandle)
        {
            exception.EnsureIsNotNull("exception");
            loggingHandle.EnsureIsNotNull("loggingHandle");

            loggingHandle().ToErrorLog(exception);
        }

        public static void ToErrorLog(this string message, Exception exception, Func<ILogger> loggingHandle)
        {
            message.EnsureIsNotNull("message");
            exception.EnsureIsNotNull("exception");
            loggingHandle.EnsureIsNotNull("loggingHandle");

            loggingHandle().ToErrorLog(message, exception);
        }
    }
}