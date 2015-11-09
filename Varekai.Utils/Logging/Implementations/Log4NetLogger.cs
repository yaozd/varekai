using System;
using log4net;
using log4net.Config;

namespace Varekai.Utils.Logging.Implementations
{
    public class Log4NetLogger : ILogger
    {
        static bool _configured;

        readonly ILog _logger;

        public Log4NetLogger(string appName)
        {
            if (!_configured)
            {
                BasicConfigurator.Configure();
                Log4NetLogger._configured = true;
            }

            _logger = LogManager.GetLogger(appName);
        }


        #region ILogger implementation

        public void ToDebugLog(string message)
        {
            _logger.Debug(message);
        }

        public void ToInfoLog(string message)
        {
            _logger.Info(message);
        }

        public void ToWarningLog(string message)
        {
            _logger.Warn(message);
        }

        public void ToErrorLog(string message)
        {
            _logger.Error(message);
        }

        public void ToErrorLog(Exception ex)
        {
            _logger.Error(ex);
        }

        public void ToErrorLogDigest(Exception ex)
        {
            _logger.Error(string.Format("{0} - {1}", ex.GetType().FullName, ex.Message));
        }

        public void ToErrorLog(AggregateException ex)
        {
            //  TODO: write all the inner exceptions
            _logger.Error(ex.Flatten());
        }

        public void ToErrorLog(string message, Exception ex)
        {
            //  TODO: write this message
            _logger.Error(ex);
        }

        #endregion
    }
}