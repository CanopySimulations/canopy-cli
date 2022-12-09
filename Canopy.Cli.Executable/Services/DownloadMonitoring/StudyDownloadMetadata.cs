using Canopy.Cli.Shared;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public record StudyDownloadMetadata(string SimVersion)
    {
        public static StudyDownloadMetadata Random() => new (
                SingletonRandom.Instance.NextString());
    }
}