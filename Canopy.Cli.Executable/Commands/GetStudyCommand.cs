using Canopy.Api.Client;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Commands
{
    public class GetStudyCommand : CanopyCommandBase
    {
        private readonly CommandOption outputFolderOption;
        private readonly CommandOption studyIdOption;
        private readonly CommandOption jobIndexOption;
        private readonly CommandOption jobIdOption;
        private readonly CommandOption channelOption;

        private readonly CommandOption rawOption;

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

            this.rawOption = this.Option(
                "--raw",
                $"Do not perform any processing on downloaded data (if omitted the binary files will be converted to CSV with unit conversion applied).",
                CommandOptionType.NoValue);
        }

        protected override async Task<int> ExecuteAsync()
        {
            var outputFolder = Utilities.GetCreatedOutputFolder(this.outputFolderOption);


            
            var studyId = this.studyIdOption.ValueOrPrompt("Study ID: ", "Study ID is required.");

            var jobIndex = this.jobIndexOption.Value();
            if (string.IsNullOrWhiteSpace(jobIndex))
            {
                jobIndex = null;
            }

            if(jobIndex != null && !uint.TryParse(jobIndex, out uint _))
            {
                throw new RecoverableException("Job index must be a valid non-negative integer.");
            }

            var keepBinaryFiles = this.rawOption.HasValue();

            var studyClient = new StudyClient(this.configuration);
            var studyMetadata = await studyClient.GetStudyMetadataWithoutUserIdAsync(this.authenticatedUser.TenantId, studyId);

            Console.WriteLine(studyMetadata.AccessInformation.Url);
            
            Console.WriteLine("This command is not yet complete.");
            //var container = new CloudBlobContainer(new Uri(studyMetadata.AccessInformation.)

            return 0;
        }

        private class StudyDownloadTask
        {
            public StudyDownloadTask(string studyId, bool downloadStudyData, IReadOnlyList<StudyJobDownloadTask> jobs)
            {
                this.DownloadStudyData = downloadStudyData;
            }

            public string StudyId { get; }

            public bool DownloadStudyData { get; }
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
