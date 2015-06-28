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
                .MinimumLevel
                    .Verbose()
                .WriteTo
                    .ColoredConsole()
                .WriteTo
                    .RollingFile(
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

        public void ToErrorLog(AggregateException ex)
        {
            //  TODO: write all the inner exceprions
            var flat = ex.Flatten();

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

