using Canopy.Api.Client;
using Canopy.Cli.Executable.Azure;
using Canopy.Cli.Executable.Services;
using Humanizer;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public class DownloadStudy : IDownloadStudy
    {
        private const string FileQuantityWord = "file";
        private static readonly TimeSpan ProgressLogRateLimit = TimeSpan.FromSeconds(5);

        private readonly IStudyClient studyClient;
        private readonly IDownloadBlobDirectory downloadBlobDirectory;
        private readonly IRetryPolicies retryPolicies;
        private readonly IGetAllRequiredDirectoryMetadata getAllRequiredDirectoryMetadata;
        private readonly IGetStudyBlobDirectory getStudyBlobDirectory;
        private readonly ILogger<DownloadStudy> logger;

        public DownloadStudy(
            IStudyClient studyClient,
            IDownloadBlobDirectory downloadBlobDirectory,
            IRetryPolicies retryPolicies,
            IGetAllRequiredDirectoryMetadata getAllRequiredDirectoryMetadata,
            IGetStudyBlobDirectory getStudyBlobDirectory,
            ILogger<DownloadStudy> logger)
        {
            this.studyClient = studyClient;
            this.downloadBlobDirectory = downloadBlobDirectory;
            this.retryPolicies = retryPolicies;
            this.getAllRequiredDirectoryMetadata = getAllRequiredDirectoryMetadata;
            this.getStudyBlobDirectory = getStudyBlobDirectory;
            this.logger = logger;
        }

        public async Task<GetStudyQueryResult> ExecuteAsync(string outputFolder, string tenantId, string studyId, int? jobIndex, CancellationToken cancellationToken)
        {
            var studyMetadata = await this.studyClient.GetStudyMetadataAsync(tenantId, studyId, cancellationToken);

            var directoriesMetadata = this.getAllRequiredDirectoryMetadata.Execute(studyMetadata, outputFolder, jobIndex);

            var directories = directoriesMetadata.Select(v => new BlobDirectoryAndOutputFolder(
                this.getStudyBlobDirectory.Execute(v.AccessInformation),
                v.OutputFolder)).ToList();

            bool isRetry = false;
            await this.retryPolicies.DownloadPolicy.ExecuteAsync(async () =>
            {
                var counters = new TransferCounters();
                var bytesPerTransfer = new long[directories.Count];

                var totalBytesProgress = new RateLimitedProgress<long>(
                    ProgressLogRateLimit,
                    totalBytes => LogTransferProgress(totalBytes, counters.Completed));

                var totalStopwatch = Stopwatch.StartNew();

                using var globalSemaphore = new SemaphoreSlim(Environment.ProcessorCount * 16);

                var tasks = directories
                    .Select((item, idx) =>
                    {
                        this.logger.LogInformation("Adding {0}", item.Directory.Container.Uri.Host);
                        return this.downloadBlobDirectory.ExecuteAsync(
                            item.Directory,
                            item.OutputFolder,
                            CreateDownloadOptions(idx, bytesPerTransfer, totalBytesProgress, counters, isRetry, globalSemaphore),
                            cancellationToken);
                    })
                    .ToList();

                isRetry = true;
                this.logger.LogInformation("Processing added storage servers...");

                var startStopwatch = Stopwatch.StartNew();
                await Task.WhenAll(tasks);
                this.logger.LogInformation("Transfer phase: {0:0.0}s", startStopwatch.Elapsed.TotalSeconds);
                this.logger.LogInformation("Total transfer time: {0:0.0}s", totalStopwatch.Elapsed.TotalSeconds);

                if (!cancellationToken.IsCancellationRequested)
                {
                    LogTransferProgress(bytesPerTransfer.Sum(), counters.Completed);

                    if (counters.Failed > 0)
                        this.logger.LogWarning("Failed downloading {0}", FileQuantityWord.ToQuantity(counters.Failed, "N0"));

                    if (counters.Skipped > 0)
                        this.logger.LogWarning("Skipped downloading {0}", FileQuantityWord.ToQuantity(counters.Skipped, "N0"));
                }
            });

            return studyMetadata;
        }

        private BlobDirectoryDownloadOptions CreateDownloadOptions(
            int idx,
            long[] bytesPerTransfer,
            RateLimitedProgress<long> totalBytesProgress,
            TransferCounters counters,
            bool isRetry,
            SemaphoreSlim globalSemaphore)
        {
            return new BlobDirectoryDownloadOptions
            {
                SkipExistingFiles = isRetry,
                ConcurrencySemaphore = globalSemaphore,
                BytesProgress = totalForDir =>
                {
                    Interlocked.Exchange(ref bytesPerTransfer[idx], totalForDir);
                    totalBytesProgress.Report(bytesPerTransfer.Sum());
                },
                OnFileCompleted = counters.IncrementCompleted,
                OnFileFailed = counters.IncrementFailed,
                OnFileSkipped = counters.IncrementSkipped,
            };
        }

        private void LogTransferProgress(long bytesTransferred, long filesTransferred)
        {
            this.logger.LogInformation("Copied: {0}, {1}", bytesTransferred.Bytes().Humanize("0.0"), FileQuantityWord.ToQuantity(filesTransferred, "N0"));
        }
    }
}
