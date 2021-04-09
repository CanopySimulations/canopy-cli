using Canopy.Api.Client;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.DataMovement;
using System.Linq;
using System.Text.RegularExpressions;
using Canopy.Cli.Executable.Helpers;

namespace Canopy.Cli.Executable.Commands
{

    public class GetStudyCommand : CanopyCommandBase
    {
        private readonly ProcessLocalStudyResults processLocalStudyResults = new ProcessLocalStudyResults();

        private readonly CommandOption outputFolderOption;
        private readonly CommandOption studyIdOption;

        private readonly CommandOption generateCsvFilesOption;
        private readonly CommandOption keepBinaryFilesOption;

        public GetStudyCommand()
        {
            this.Name = "get-study";
            this.Description = "Downloads the specified study or study job.";

            this.outputFolderOption = this.Option(
                "-o | --output-folder",
                $"The output folder in which to save the files (defaults to the current directory).",
                CommandOptionType.SingleValue);

            this.studyIdOption = this.Option(
                "-s | --study-id",
                $"The study to download.",
                CommandOptionType.SingleValue);

            this.generateCsvFilesOption = this.Option(
                "-csv | --generate-csv",
                $"Generate CSV files from binary files.",
                CommandOptionType.NoValue);

            this.keepBinaryFilesOption = this.Option(
                "-bin | --keep-binary",
                $"Do not delete binary files which have been processed into CSV files (faster).",
                CommandOptionType.NoValue);
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
                if((now - this.lastReport) < this.rate)
                {
                    return;
                }

                this.lastReport = now;
                this.inner.Report(value);
            }
        }

        protected override async Task<int> ExecuteAsync()
        {
            var outputFolder = Utilities.GetCreatedOutputFolder(this.outputFolderOption);
            
            var studyId = this.studyIdOption.ValueOrPrompt("Study ID: ", "Study ID is required.");

            var deleteProcessedFiles = !this.keepBinaryFilesOption.HasValue();
            var genarateCsvFiles = this.generateCsvFilesOption.HasValue();

            var studyClient = new StudyClient(this.configuration);
            var studyMetadata = await studyClient.GetStudyMetadataAsync(this.authenticatedUser.TenantId, studyId);

            // TODO: Handle expiration of access signatures.
            var directories = this.GetAllStudyBlobDirectories(studyMetadata.AccessInformation);

            TransferManager.Configurations.ParallelOperations = System.Net.ServicePointManager.DefaultConnectionLimit;
            TransferManager.Configurations.MaxListingConcurrency = 20;
            Console.WriteLine($"Using {TransferManager.Configurations.ParallelOperations} parallel operations.");
            Console.WriteLine($"Using {TransferManager.Configurations.MaxListingConcurrency} max listing concurrency.");
            TransferManager.Configurations.BlockSize = 20971520;
            var tasks = new List<Task>();
            var progressRateLimit = TimeSpan.FromSeconds(1);
            foreach (var directory in directories)
            {
                Console.WriteLine($"Adding {directory.Uri.Host}");
               
                var context = new DirectoryTransferContext();
                context.ProgressHandler = new RateLimitedProgress<TransferStatus>(
                    progressRateLimit, 
                    new Progress<TransferStatus>(progress =>
                        {
                            Console.WriteLine($"{directory.Uri.Host} Bytes Copied: {progress.BytesTransferred}");
                            //Console.WriteLine($"{directory.Uri.Host} Files Copied: {progress.NumberOfFilesTransferred}");
                        }));

                tasks.Add(TransferManager.DownloadDirectoryAsync(
                    directory,
                    outputFolder,
                    new DownloadDirectoryOptions {Recursive = true},
                    context));
            }

            await Task.WhenAll(tasks);

            if (genarateCsvFiles)
            {
                await this.processLocalStudyResults.ExecuteAsync(outputFolder, deleteProcessedFiles);
            }

            return 0;
        }

        private IReadOnlyList<CloudBlobDirectory> GetAllStudyBlobDirectories(StudyBlobAccessInformation accessInformation)
        {
            var mainDirectory = this.GetStudyBlobDirectory(accessInformation.Url, accessInformation.AccessSignature);

            var jobDirectories = accessInformation.Jobs.Select(v => this.GetStudyBlobDirectory(v.Url, v.AccessSignature));

            return new List<CloudBlobDirectory> {mainDirectory}
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
    }
}
