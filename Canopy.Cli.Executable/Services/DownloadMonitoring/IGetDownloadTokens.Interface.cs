using System.Collections.Generic;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IGetDownloadTokens
    {
        IEnumerable<string> Execute(string folderPath);
    }
}