using System.IO;
namespace Canopy.Cli.Executable.Services
{
    using System;
    using Microsoft.Extensions.Logging;
    using Polly;
    using Polly.Retry;

    public class RetryPolicies : IRetryPolicies
    {
        private readonly ILogger<RetryPolicies> logger;

        public RetryPolicies(ILogger<RetryPolicies> logger)
        {
            this.logger = logger;

            this.FilePolicy = Policy
                .Handle<IOException>()
                .WaitAndRetryAsync(
                    new[]
                    {
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(3)
                    },
                    (exception, timeSpan, context) =>
                    {
                        this.logger.LogInformation("Retrying file IO operation.");
                    });

            this.DownloadPolicy = Policy
                .Handle<Exception>(v => !ExceptionUtilities.IsFromCancellation(v))
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, context) =>
                    {
                        this.logger.LogWarning(exception, "Retrying download.");
                    });
        }

        public AsyncRetryPolicy FilePolicy { get; init; }

        public AsyncRetryPolicy DownloadPolicy { get; init; }
    }
}