using System;

namespace Varekai.Utils.Logging.Implementations
{
    public struct SerilogRollingFileConfiguration
    {
        public readonly string FilePath;
        public readonly int? NumberOfFilesToKeep;
        public readonly long? FileSize;
        
        public SerilogRollingFileConfiguration(
            string path,
            long? fileSize = null,
            int? filesToKeep = null) : this()
        {
            FilePath = path;
            FileSize = fileSize;
            NumberOfFilesToKeep = filesToKeep;
        }
    }
}

