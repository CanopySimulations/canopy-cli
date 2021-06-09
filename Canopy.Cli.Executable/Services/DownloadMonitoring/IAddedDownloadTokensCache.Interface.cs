using System.IO;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IAddedDownloadTokensCache
    {
        bool TryAdd(string filePath);
    }
}