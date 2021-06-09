using Canopy.Cli.Shared;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public record DownloadToken(string TenantId, string StudyId, string StudyName, string? JobName)
    {
        public static DownloadToken Random()
        {
            return new DownloadToken(
                SingletonRandom.Instance.NextString(),
                SingletonRandom.Instance.NextString(),
                SingletonRandom.Instance.NextString(),
                SingletonRandom.Instance.NextBoolean() ? SingletonRandom.Instance.NextString() : null);
        }
    }
}