using System;
using Serilog;

namespace Varekai.Utils.Logging.Implementations
{
    public class SerilogLogger : ILogger
    {
        readonly Serilog.ILogger _logger;

        public static LoggerConfiguration CreateDefaultConfiguration(SerilogRollingFileConfiguration fileConfiguration)
        {
            return new LoggerConfiguration()
                .Enrich.WithThreadId()
                .MinimumLevel.ControlledBy(fileConfiguration.GetLoggingLevelSwitch())
                .WriteTo.ColoredConsole(
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} - {ThreadId} - [{Level}] {Message}{NewLine}{Exception}")
                .WriteTo.RollingFile(
                    fileConfiguration.FilePath,
                    fileSizeLimitBytes: fileConfiguration.FileSize,
                    retainedFileCountLimit: fileConfiguration.NumberOfFilesToKeep);
        }

        public SerilogLogger(SerilogRollingFileConfiguration fileConfiguration)
        {
            _logger = 
                CreateDefaultConfiguration(fileConfiguration)
                .CreateLogger();
        }

        #region ILogger implementation

        public void ToDebugLog(string message)
        {
            _logger.Debug(message);
        }

        public void ToInfoLog(string message)
        {
            _logger.Information(message);
        }

        public void ToWarningLog(string message)
        {
            _logger.Warning(message);
        }

        public void ToErrorLog(string message)
        {
            _logger.Error(message);
        }

        public void ToErrorLog(Exception ex)
        {
            _logger.Error(ex, ex.Message);
        }

        public void ToErrorLogDigest(Exception ex)
        {
            _logger.Error(string.Format("{0} - {1}", ex.GetType().FullName, ex.Message));
        }

        public void ToErrorLog(AggregateException ex)
        {
            //  TODO: write all the inner exceptions
            _logger.Error(ex, ex.Message);
        }

        public void ToErrorLog(string message, Exception ex)
        {
            //  TODO: write this message
            _logger.Error(ex, message);
        }

        #endregion
    }
}

