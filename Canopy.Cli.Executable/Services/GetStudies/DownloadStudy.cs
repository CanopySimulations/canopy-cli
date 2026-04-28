using Azure.Storage.DataMovement;
using Canopy.Api.Client;
using Humanizer;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
                var options = CreateOptions();

                var tasks = new List<Task<TransferOperation?>>();

                foreach (var item in directories)
                {
                    this.logger.LogInformation("Adding {0}", item.Directory.Container.Uri.Host);

                    tasks.Add(this.downloadBlobDirectory.ExecuteAsync(
                        item.Directory,
                        item.OutputFolder,
                        isRetry,
                        options,
                        cancellationToken));
                }

                isRetry = true;

                this.logger.LogInformation("Processing added storage servers...");


                // Wait for all transfers to finish
                var transferOperations = await Task.WhenAll(tasks.Where(t => t != null));
                await Task.WhenAll(
                    transferOperations
                        .Where(op => op != null)
                        .Select(op => op!.WaitForCompletionAsync(cancellationToken))
                );


                //var filesTransferred =
                //    transferOperations.Sum(t => t?.Status.CompletedTransfers ?? 0);

                //var filesFailed =
                //    transfers.Sum(t => t?.Status.FailedTransfers ?? 0);

                //var filesSkipped =
                //    transfers.Sum(t => t?.Status.SkippedTransfers ?? 0);

                //var bytesTransferred = results.Sum(v => v?.BytesTransferred ?? 0);

                //if (!cancellationToken.IsCancellationRequested)
                //{
                //    LogTransferProgress(bytesTransferred, filesTransferred);

                //    if (filesFailed > 0)
                //    {
                //        this.logger.LogWarning("Failed downloading {0}", FileQuantityWord.ToQuantity(filesFailed, "N0"));
                //    }

                //    if (filesSkipped > 0)
                //    {
                //        this.logger.LogWarning("Skipped downloading {0}", FileQuantityWord.ToQuantity(filesSkipped, "N0"));
                //    }
                //}
            });

            return studyMetadata;
        }

        private TransferProgressHandlerOptions CreateOptions()
        {
            var progressRateLimit = TimeSpan.FromSeconds(5);
            var context = new TransferProgressHandlerOptions();
            context.ProgressHandler = new RateLimitedProgress<TransferProgress>(
                progressRateLimit,
                new Progress<TransferProgress>(progress =>
                {
                    var bytesTransferred = progress.BytesTransferred ?? 0;
                    var filesTransferred = progress.CompletedCount;
                    LogTransferProgress(bytesTransferred, filesTransferred);
                }));

            //context.FileFailed += (s, e) => this.logger.LogWarning("Failed to download {0}", e.Destination);
            context.TrackBytesTransferred = true;
            //context.ShouldOverwriteCallbackAsync = TransferContext.ForceOverwrite;

            return context;
        }
        private void LogTransferProgress(long bytesTransferred, long filesTransferred)
        {
            this.logger.LogInformation("Copied: {0}, {1}", bytesTransferred.Bytes().Humanize("0.0"), FileQuantityWord.ToQuantity(filesTransferred, "N0"));
        }

        public class RateLimitedProgress<T> : IProgress<T>
        {
            private readonly IProgress<T> inner;
            private readonly TimeSpan rate;

            private DateTimeOffset lastReport = DateTimeOffset.MinValue;

            public RateLimitedProgress(TimeSpan rate, IProgress<T> inner)
            {
                this.inner = inner;
                this.rate = rate;
            }

            public void Report(T value)
            {
                var now = DateTimeOffset.UtcNow;
                if ((now - this.lastReport) < this.rate)
                {
                    return;
                }

                this.lastReport = now;
                this.inner.Report(value);
            }
        }
    }
}