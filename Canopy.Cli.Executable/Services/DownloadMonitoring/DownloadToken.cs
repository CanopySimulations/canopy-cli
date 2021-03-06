using Canopy.Cli.Shared;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public record DownloadToken(string TenantId, string StudyId, string StudyName, DownloadTokenJob? Job)
    {
        public static DownloadToken Random()
        {
            return new DownloadToken(
                SingletonRandom.Instance.NextString(),
                SingletonRandom.Instance.NextString(),
                SingletonRandom.Instance.NextString(),
                SingletonRandom.Instance.NextBoolean() ? DownloadTokenJob.Random() : null);
        }
    }
}