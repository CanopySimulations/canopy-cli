using Canopy.Cli.Shared;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public record QueuedDownloadToken(string TokenPath, DownloadToken Token)
    {
        public static QueuedDownloadToken Random()
        {
            return new QueuedDownloadToken(
                SingletonRandom.Instance.NextString(),
                DownloadToken.Random());
        }
    }
}