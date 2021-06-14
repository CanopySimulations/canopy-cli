using Canopy.Cli.Shared;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public record DownloadTokenJob(string JobId, string JobName)
    {
        public static DownloadTokenJob Random()
        {
            return new DownloadTokenJob(
                SingletonRandom.Instance.NextString(),
                SingletonRandom.Instance.NextString());
            }
    }
}