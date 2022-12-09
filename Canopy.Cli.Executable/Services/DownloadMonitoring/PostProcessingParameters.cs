using Canopy.Cli.Shared;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public record PostProcessingParameters(
        string PostProcessorPath,
        string PostProcessorArguments,
        string DecryptingTenantShortName)
    {
        public static PostProcessingParameters Random() => new (
                SingletonRandom.Instance.NextString(),
                SingletonRandom.Instance.NextString(),
                SingletonRandom.Instance.NextString());
    }
}