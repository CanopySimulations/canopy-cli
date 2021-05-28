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

namespace Canopy.Cli.Executable.Services
{
    public class GetStudy : IGetStudy
    {
        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly IProcessLocalStudyResults processLocalStudyResults;
        private readonly IStudyClient studyClient;
        private readonly ILogger<GetStudy> logger;

        public GetStudy(
            IEnsureAuthenticated ensureAuthenticated,
            IProcessLocalStudyResults processLocalStudyResults,
            IStudyClient studyClient,
            ILogger<GetStudy> logger)
        {
            this.ensureAuthenticated = ensureAuthenticated;
            this.processLocalStudyResults = processLocalStudyResults;
            this.studyClient = studyClient;
            this.logger = logger;
        }

        public async Task ExecuteAsync(GetStudyCommand.Parameters parameters)
        {
            var authenticatedUser = await this.ensureAuthenticated.ExecuteAsync();

            var outputFolder = Utilities.GetCreatedOutputFolder(parameters.OutputFolder);

            var studyId = parameters.StudyId;

            var deleteProcessedFiles = !parameters.KeepBinary;
            var generateCsvFiles = parameters.GenerateCsv;

            var studyMetadata = await this.studyClient.GetStudyMetadataAsync(authenticatedUser.TenantId, studyId);

            // TODO: Handle expiration of access signatures.
            var directories = this.GetAllStudyBlobDirectories(studyMetadata.AccessInformation);

            this.logger.LogInformation("Using {0} parallel operations.", TransferManager.Configurations.ParallelOperations);
            this.logger.LogInformation("Using {0} max listing concurrency.", TransferManager.Configurations.MaxListingConcurrency);
            var tasks = new List<Task>();
            var progressRateLimit = TimeSpan.FromSeconds(1);
            foreach (var directory in directories)
            {
                this.logger.LogInformation("Adding {0}", directory.Uri.Host);

                var context = new DirectoryTransferContext();
                context.ProgressHandler = new RateLimitedProgress<TransferStatus>(
                    progressRateLimit,
                    new Progress<TransferStatus>(progress =>
                        {
                            this.logger.LogInformation("{0} Bytes Copied: {1}", directory.Uri.Host, progress.BytesTransferred);
                            //this.logger.LogInformation($"{0} Files Copied: {1}", directory.Uri.Host, progress.NumberOfFilesTransferred);
                        }));

                tasks.Add(TransferManager.DownloadDirectoryAsync(
                    directory,
                    outputFolder,
                    new DownloadDirectoryOptions { Recursive = true },
                    context));
            }

            await Task.WhenAll(tasks);

            if (generateCsvFiles)
            {
                await this.processLocalStudyResults.ExecuteAsync(outputFolder, deleteProcessedFiles);
            }
        }

        private IReadOnlyList<CloudBlobDirectory> GetAllStudyBlobDirectories(StudyBlobAccessInformation accessInformation)
        {
            var mainDirectory = this.GetStudyBlobDirectory(accessInformation.Url, accessInformation.AccessSignature);

            var jobDirectories = accessInformation.Jobs.Select(v => this.GetStudyBlobDirectory(v.Url, v.AccessSignature));

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