using System.Collections.Generic;
using System.IO;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class GetDownloadTokens : IGetDownloadTokens
    {
        public IEnumerable<string> Execute(string folderPath)
        {
            return Directory.EnumerateFiles(folderPath, $"*{DownloaderConstants.DownloadTokenExtensionWithPeriod}");
        }
    }
}