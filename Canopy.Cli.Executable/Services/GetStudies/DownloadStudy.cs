using Azure.Storage.DataMovement;
using Canopy.Api.Client;
using Canopy.Cli.Executable.Azure;
using Humanizer;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public class DownloadStudy : IDownloadStudy
    {
        private const string FileQuantityWord = "file";
        private const long MaximumTransferChunkSize = 20_971_520;
        private static readonly TimeSpan ProgressLogRateLimit = TimeSpan.FromSeconds(5);

        private readonly IStudyClient studyClient;
        private readonly IDownloadBlobDirectory downloadBlobDirectory;
        private readonly IRetryPolicies retryPolicies;
        private readonly IGetAllRequiredDirectoryMetadata getAllRequiredDirectoryMetadata;
        private readonly IGetStudyBlobDirectory getStudyBlobDirectory;
        private readonly TransferManagerOptions transferManagerOptions;
        private readonly ILogger<DownloadStudy> logger;

        public DownloadStudy(
            IStudyClient studyClient,
            IDownloadBlobDirectory downloadBlobDirectory,
            IRetryPolicies retryPolicies,
            IGetAllRequiredDirectoryMetadata getAllRequiredDirectoryMetadata,
            IGetStudyBlobDirectory getStudyBlobDirectory,
            TransferManagerOptions transferManagerOptions,
            ILogger<DownloadStudy> logger)
        {
            this.studyClient = studyClient;
            this.downloadBlobDirectory = downloadBlobDirectory;
            this.retryPolicies = retryPolicies;
            this.getAllRequiredDirectoryMetadata = getAllRequiredDirectoryMetadata;
            this.getStudyBlobDirectory = getStudyBlobDirectory;
            this.logger = logger;
            this.transferManagerOptions = transferManagerOptions;
        }

        public async Task<GetStudyQueryResult> ExecuteAsync(string outputFolder, string tenantId, string studyId, int? jobIndex, CancellationToken cancellationToken)
        {
            var studyMetadata = await this.studyClient.GetStudyMetadataAsync(tenantId, studyId, cancellationToken);

            var directoriesMetadata = this.getAllRequiredDirectoryMetadata.Execute(studyMetadata, outputFolder, jobIndex);

            var directories = directoriesMetadata.Select(v => new BlobDirectoryAndOutputFolder(
                this.getStudyBlobDirectory.Execute(v.AccessInformation),
                v.OutputFolder)).ToList();

            this.logger.LogInformation("Using {0} parallel operations.", this.transferManagerOptions.MaximumConcurrency);

            bool isRetry = false;
            await this.retryPolicies.DownloadPolicy.ExecuteAsync(async () =>
            {
                var counters = new TransferCounters();
                var bytesPerTransfer = new long[directories.Count];

                var totalBytesProgress = new RateLimitedProgress<long>(
                    ProgressLogRateLimit,
                    new Progress<long>(totalBytes => LogTransferProgress(totalBytes, counters.Completed)));

                var tasks = directories
                    .Select((item, idx) =>
                    {
                        this.logger.LogInformation("Adding {0}", item.Directory.Container.Uri.Host);
                        return this.downloadBlobDirectory.ExecuteAsync(
                            item.Directory,
                            item.OutputFolder,
                            CreateTransferOptions(idx, bytesPerTransfer, totalBytesProgress, counters, isRetry),
                            cancellationToken);
                    })
                    .ToList();

                isRetry = true;
                this.logger.LogInformation("Processing added storage servers...");

                await Task.WhenAll(tasks);

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

        private TransferOptions CreateTransferOptions(
            int idx,
            long[] bytesPerTransfer,
            IProgress<long> totalBytesProgress,
            TransferCounters counters,
            bool isRetry)
        {
            var options = new TransferOptions
            {
                CreationMode = isRetry
                    ? StorageResourceCreationMode.SkipIfExists
                    : StorageResourceCreationMode.OverwriteIfExists,
                MaximumTransferChunkSize = MaximumTransferChunkSize,
                ProgressHandlerOptions = new TransferProgressHandlerOptions
                {
                    TrackBytesTransferred = true,
                    ProgressHandler = new Progress<TransferProgress>(p =>
                    {
                        Interlocked.Exchange(ref bytesPerTransfer[idx], p.BytesTransferred ?? 0);
                        totalBytesProgress.Report(bytesPerTransfer.Sum());
                    })
                }
            };

            options.ItemTransferCompleted += _ => { counters.IncrementCompleted(); return Task.CompletedTask; };
            options.ItemTransferFailed    += _ => { counters.IncrementFailed();    return Task.CompletedTask; };
            options.ItemTransferSkipped   += _ => { counters.IncrementSkipped();   return Task.CompletedTask; };

            return options;
        }

        private void LogTransferProgress(long bytesTransferred, long filesTransferred)
        {
            this.logger.LogInformation("Copied: {0}, {1}", bytesTransferred.Bytes().Humanize("0.0"), FileQuantityWord.ToQuantity(filesTransferred, "N0"));
        }

    }
}