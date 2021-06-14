using Canopy.Cli.Shared;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public record DownloadTokenJob(int JobIndex, string JobName)
    {
        public static DownloadTokenJob Random()
        {
            return new DownloadTokenJob(
                SingletonRandom.Instance.NextInclusive(0, 1000),
                SingletonRandom.Instance.NextString());
            }
    }
}