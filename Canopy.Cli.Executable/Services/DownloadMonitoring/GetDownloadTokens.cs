using System.Collections.Generic;
using System.IO;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class GetDownloadTokens : IGetDownloadTokens
    {
        public IEnumerable<string> Execute(string folderPath)
        {
            // Recursive is false because:
            //  - Often completed downloads are under the folder being monitored.
            //  - The file watcher isn't monitoring recursively or large downloads cause it problems.
            return Directory.EnumerateFiles(
                folderPath, 
                $"*{DownloaderConstants.DownloadTokenExtensionWithPeriod}", 
                new EnumerationOptions { RecurseSubdirectories = false });
        }
    }
}