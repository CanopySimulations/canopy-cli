using Canopy.Api.Client;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Commands
{
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Web;
    using Canopy.Cli.Executable.Helpers;
    using Canopy.Cli.Shared;

    public class GetStudyCommand : CanopyCommandBase
    {
        private readonly ProcessLocalStudyResults processLocalStudyResults = new ProcessLocalStudyResults();

        private readonly CommandOption outputFolderOption;
        private readonly CommandOption studyIdOption;
        //private readonly CommandOption jobIndexOption;
        //private readonly CommandOption jobIdOption;
        //private readonly CommandOption channelOption;

        private readonly CommandOption keepOriginalFilesOption;

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
            /*
            this.jobIndexOption = this.Option(
                "-i | --job-index",
                $"The study job index to download (if omitted the entire study is downloaded).",
                CommandOptionType.MultipleValue);

            this.jobIdOption = this.Option(
                "-j | --job-id",
                $"The job id to download (study ID is not required if the job ID is given).",
                CommandOptionType.MultipleValue);

            this.channelOption = this.Option(
                "-c | --channel",
                $"The channel or channels to download (if omitted all channels are downloaded).",
                CommandOptionType.MultipleValue);
            */
            this.keepOriginalFilesOption = this.Option(
                "-ko | --keep-original",
                $"Do not delete files which have been processed (faster).",
                CommandOptionType.NoValue);
        }

        protected override async Task<int> ExecuteAsync()
        {
            var outputFolder = Utilities.GetCreatedOutputFolder(this.outputFolderOption);
            
            var studyId = this.studyIdOption.ValueOrPrompt("Study ID: ", "Study ID is required.");

            /*
            var jobIndex = this.jobIndexOption.Value();
            if (string.IsNullOrWhiteSpace(jobIndex))
            {
                jobIndex = null;
            }

            if(jobIndex != null && !uint.TryParse(jobIndex, out uint _))
            {
                throw new RecoverableException("Job index must be a valid non-negative integer.");
            }
            */

            var deleteProcessedFiles = !this.keepOriginalFilesOption.HasValue();

            var studyClient = new StudyClient(this.configuration);
            var studyMetadata = await studyClient.GetStudyMetadataWithoutUserIdAsync(this.authenticatedUser.TenantId, studyId);

            // TODO: Handle expiration of access signatures.
            var directories = this.GetAllStudyBlobDirectories(studyMetadata.AccessInformation);

            const int crossServerParallelism = 4;
            const int perServerParallelism = 2;
            await directories.ForEachAsync(crossServerParallelism, async directory =>
            {
                BlobContinuationToken continuationToken = null;
                do
                {
                    var segment = await directory.ListBlobsSegmentedAsync(true, BlobListingDetails.None, 1000, continuationToken, null, null);

                    await segment.Results.OfType<CloudBlockBlob>().ForEachAsync(perServerParallelism, async blob =>
                    {
                        Console.Write(".");
                        var relativeUri = directory.Uri.MakeRelativeUri(blob.Uri).ToString();
                        relativeUri = HttpUtility.UrlDecode(relativeUri);
                        var filePath = Path.Combine(outputFolder, relativeUri);
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        await blob.DownloadToFileAsync(filePath, FileMode.Create);
                    });

                    continuationToken = segment.ContinuationToken;
                }
                while (continuationToken != null);
            });

            await this.processLocalStudyResults.ExecuteAsync(outputFolder, deleteProcessedFiles);

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
            var containerUrlMatch = Regex.Match(url, $@"^(?<{containerUrlKey}>.*)/(?<{studyPathKey}>[\w]*/[\w-]*/)$");
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

        private class StudyDownloadTask
        {
            public StudyDownloadTask(string studyId, bool downloadStudyData, IReadOnlyList<StudyJobDownloadTask> jobs)
            {
                this.StudyId = studyId;
                this.DownloadStudyData = downloadStudyData;
                this.Jobs = jobs;
            }

            public string StudyId { get; }

            public bool DownloadStudyData { get; }

            public IReadOnlyList<StudyJobDownloadTask> Jobs { get; }
        }

        private class StudyJobDownloadTask
        {
            public StudyJobDownloadTask(string jobId)
            {
                this.JobId = jobId;
                var hyphenIndex = jobId.IndexOf('-');
                if(hyphenIndex == -1)
                {
                    throw new RecoverableException("Invalid job ID: " + jobId);
                }

                this.StudyId = jobId.Substring(0, hyphenIndex);

                var jobIndexString = jobId.Substring(hyphenIndex + 1);
                int jobIndex;
                if(!int.TryParse(jobIndexString, out jobIndex))
                {
                    throw new RecoverableException("Invalid job index: " + jobIndex);
                }

                this.JobIndex = jobIndex;
            }

            public StudyJobDownloadTask(string studyId, int jobIndex)
            {
                this.StudyId = studyId;
                this.JobIndex = jobIndex;
                this.JobId = $"{studyId}-{jobIndex}";
            }

            public string StudyId { get; }

            public string JobId { get; }

            public int JobIndex { get; }
        }
    }
}
