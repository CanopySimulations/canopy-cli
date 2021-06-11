using Canopy.Api.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.DataMovement;
using System.Linq;
using System.Text.RegularExpressions;
using Canopy.Cli.Executable.Commands;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Humanizer;
using System.Threading;

namespace Canopy.Cli.Executable.Services
{
    public class GetStudy : IGetStudy
    {
        private const string FileQuantityWord = "file";

        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly IProcessLocalStudyResults processLocalStudyResults;
        private readonly IStudyClient studyClient;
        private readonly IGetCreatedOutputFolder getCreatedOutputFolder;
        private readonly IDownloadBlobDirectory downloadBlobDirectory;
        private readonly IRetryPolicies retryPolicies;
        private readonly ILogger<GetStudy> logger;

        public GetStudy(
            IEnsureAuthenticated ensureAuthenticated,
            IProcessLocalStudyResults processLocalStudyResults,
            IStudyClient studyClient,
            IGetCreatedOutputFolder getCreatedOutputFolder,
            IDownloadBlobDirectory downloadBlobDirectory,
            IRetryPolicies retryPolicies,
            ILogger<GetStudy> logger)
        {
            this.ensureAuthenticated = ensureAuthenticated;
            this.processLocalStudyResults = processLocalStudyResults;
            this.studyClient = studyClient;
            this.getCreatedOutputFolder = getCreatedOutputFolder;
            this.downloadBlobDirectory = downloadBlobDirectory;
            this.retryPolicies = retryPolicies;
            this.logger = logger;
        }

        public async Task ExecuteAsync(GetStudyCommand.Parameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var authenticatedUser = await this.ensureAuthenticated.ExecuteAsync();

                var outputFolder = this.getCreatedOutputFolder.Execute(parameters.OutputFolder);

                var tenantId = string.IsNullOrWhiteSpace(parameters.TenantId) ? authenticatedUser.TenantId : parameters.TenantId;
                var studyId = parameters.StudyId;

                await this.DownloadStudyAsync(outputFolder, tenantId, studyId, cancellationToken);

                var generateCsvFiles = parameters.GenerateCsv;

                if (generateCsvFiles && !cancellationToken.IsCancellationRequested)
                {
                    var deleteProcessedFiles = !parameters.KeepBinary;
                    await this.processLocalStudyResults.ExecuteAsync(outputFolder, deleteProcessedFiles, cancellationToken);
                }
            }
            catch (Exception t) when (ExceptionUtilities.IsFromCancellation(t))
            {
                // Just return if the task was cancelled.
            }
        }

        private async Task DownloadStudyAsync(string outputFolder, string tenantId, string studyId, CancellationToken cancellationToken)
        {
            var studyMetadata = await this.studyClient.GetStudyMetadataAsync(tenantId, studyId);

            var directories = this.GetAllStudyBlobDirectories(studyMetadata);

            this.logger.LogInformation("Using {0} parallel operations.", TransferManager.Configurations.ParallelOperations);
            this.logger.LogInformation("Using {0} max listing concurrency.", TransferManager.Configurations.MaxListingConcurrency);

            DirectoryTransferContext? context = null;
            await this.retryPolicies.DownloadPolicy.ExecuteAsync(async () =>
            {
                context = context == null ? new DirectoryTransferContext() : new DirectoryTransferContext(context.LastCheckpoint);
                ConfigureContext(context);

                var tasks = new List<Task<TransferStatus?>>();
                foreach (var directory in directories)
                {
                    this.logger.LogInformation("Adding {0}", directory.Uri.Host);

                    tasks.Add(this.downloadBlobDirectory.ExecuteAsync(
                        directory,
                        outputFolder,
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

        private IReadOnlyList<CloudBlobDirectory> GetAllStudyBlobDirectories(GetStudyQueryResult studyMetadata)
        {
            var accessInformation = studyMetadata.AccessInformation;

            var mainDirectory = this.GetStudyBlobDirectory(accessInformation.Url, accessInformation.AccessSignature);

            var studyData = studyMetadata.Study.Data as JObject;
            Guard.Operation(studyData != null, "Study data was not found in study metadata result.");

            var jobCount = studyData.Value<int>(Api.Client.Constants.JobCountKey);
            var jobDirectoryCount = Math.Min(jobCount, accessInformation.Jobs.Count);
            var jobDirectories = accessInformation.Jobs.Take(jobDirectoryCount).Select(v => this.GetStudyBlobDirectory(v.Url, v.AccessSignature));

            return new List<CloudBlobDirectory> { mainDirectory }
                .Concat(jobDirectories).ToList();
        }

        private CloudBlobDirectory GetStudyBlobDirectory(string url, string accessSignature)
        {
            const string containerUrlKey = "containerUrl";
            const string studyPathKey = "studyPath";
            var containerUrlMatch = Regex.Match(url, $@"^(?<{containerUrlKey}>https://[^/]*/[\w]*)/(?<{studyPathKey}>.*)$");
            if (!containerUrlMatch.Success)
            {
                throw new RecoverableException("Unexpected study URL format: " + url);
            }

            var containerUrl = containerUrlMatch.Groups[containerUrlKey].Value;
            var studyPath = containerUrlMatch.Groups[studyPathKey].Value;

            var container = new CloudBlobContainer(new Uri(containerUrl + accessSignature));

            var directory = container.GetDirectoryReference(studyPath);
            return directory;
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