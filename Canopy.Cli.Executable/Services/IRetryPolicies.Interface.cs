namespace Canopy.Cli.Executable.Services
{
    using Polly;
    using Polly.Retry;

    public interface IRetryPolicies
    {
        AsyncRetryPolicy FilePolicy { get; }
        AsyncRetryPolicy DownloadPolicy { get; init; }
    }
}