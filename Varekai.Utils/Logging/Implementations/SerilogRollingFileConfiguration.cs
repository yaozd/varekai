using Serilog.Core;
using Serilog.Events;

namespace Varekai.Utils.Logging.Implementations
{
    public struct SerilogRollingFileConfiguration
    {
        public readonly string FilePath;
        public readonly int? NumberOfFilesToKeep;
        public readonly long? FileSize;
        public readonly LogLevels LogLevel;
        
        public SerilogRollingFileConfiguration(
            string path,
            long? fileSize = null,
            int? filesToKeep = null,
            LogLevels logLevel = LogLevels.Debug) : this()
        {
            FilePath = path;
            FileSize = fileSize;
            NumberOfFilesToKeep = filesToKeep;
            LogLevel = logLevel;
        }

        public LoggingLevelSwitch GetLoggingLevelSwitch()
        {
            return new LoggingLevelSwitch((LogEventLevel)LogLevel);
        }
    }
}

