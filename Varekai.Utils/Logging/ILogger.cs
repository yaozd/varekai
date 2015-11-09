using System;

namespace Varekai.Utils.Logging
{
    public interface ILogger
    {
        void ToDebugLog(string message);
        void ToInfoLog(string message);
        void ToWarningLog(string message);
        void ToErrorLog(string message);
        void ToErrorLog(Exception ex);
        void ToErrorLogDigest(Exception ex);
        void ToErrorLog(AggregateException ex);
        void ToErrorLog(string message, Exception ex);
    }
}

