using Canopy.Api.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.DataMovement;
using System.Linq;
using Microsoft.Extensions.Logging;
using Humanizer;
using System.Threading;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public class DownloadStudy : IDownloadStudy
    {
        private const string FileQuantityWord = "file";

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

            this.logger.LogInformation("Using {0} parallel operations.", TransferManager.Configurations.ParallelOperations);
            this.logger.LogInformation("Using {0} max listing concurrency.", TransferManager.Configurations.MaxListingConcurrency);

            DirectoryTransferContext? context = null;
            await this.retryPolicies.DownloadPolicy.ExecuteAsync(async () =>
            {
                context = context == null ? new DirectoryTransferContext() : new DirectoryTransferContext(context.LastCheckpoint);
                ConfigureContext(context);

                var tasks = new List<Task<TransferStatus?>>();
                foreach (var item in directories)
                {
                    this.logger.LogInformation("Adding {0}", item.Directory.Uri.Host);

                    tasks.Add(this.downloadBlobDirectory.ExecuteAsync(
                        item.Directory,
                        item.OutputFolder,
                        new DownloadDirectoryOptions { Recursive = true },
                        context,
                        cancellationToken));
                }

                this.logger.LogInformation("Processing added storage servers...");

                var results = await Task.WhenAll(tasks);

                var filesTransferred = results.Sum(v => v?.NumberOfFilesTransferred ?? 0);
                var filesFailed = results.Sum(v => v?.NumberOfFilesFailed ?? 0);
                var filesSkipped = results.Sum(v => v?.NumberOfFilesSkipped ?? 0);
                var bytesTransferred = results.Sum(v => v?.BytesTransferred ?? 0);

                if (!cancellationToken.IsCancellationRequested)
                {
                    LogTransferProgress(bytesTransferred, filesTransferred);

                    if (filesFailed > 0)
                    {
                        this.logger.LogWarning("Failed downloading {0}", FileQuantityWord.ToQuantity(filesFailed, "N0"));
                    }

                    if (filesSkipped > 0)
                    {
                        this.logger.LogWarning("Skipped downloading {0}", FileQuantityWord.ToQuantity(filesSkipped, "N0"));
                    }
                }
            });

            return studyMetadata;
        }

        private void ConfigureContext(DirectoryTransferContext context)
        {
            var progressRateLimit = TimeSpan.FromSeconds(5);
            context.ProgressHandler = new RateLimitedProgress<TransferStatus>(
                progressRateLimit,
                new Progress<TransferStatus>(progress =>
                {
                    var bytesTransferred = progress.BytesTransferred;
                    var filesTransferred = progress.NumberOfFilesTransferred;
                    LogTransferProgress(bytesTransferred, filesTransferred);
                }));

            context.FileFailed += (s, e) => this.logger.LogWarning("Failed to download {0}", e.Destination);

            context.ShouldOverwriteCallbackAsync = TransferContext.ForceOverwrite;
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