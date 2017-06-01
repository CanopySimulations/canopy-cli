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

        private readonly CommandOption binaryOption;

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
                "-j | --job-index",
                $"The study job index to download (if omitted the entire study is downloaded).",
                CommandOptionType.SingleValue);

            this.binaryOption = this.Option(
                "-b | --binary",
                $"Keep downloaded channels as binary files (if omitted the binary files will be converted to CSV).",
                CommandOptionType.NoValue);
        }

        protected override async Task ExecuteAsync()
        {
            var outputFolder = Utilities.GetCreatedOutputFolder(this.outputFolderOption);

            var studyId = this.studyIdOption.ValueOrPrompt("Study ID: ", "Study ID is required.");

            var jobIndex = this.jobIndexOption.Value();
            if (string.IsNullOrWhiteSpace(jobIndex))
            {
                jobIndex = null;
            }

            if(jobIndex != null && !uint.TryParse(jobIndex, out uint result))
            {
                throw new RecoverableException("Job index must be a valid non-negative integer.");
            }

            var keepBinaryFiles = this.binaryOption.HasValue();

            var studyClient = new StudyClient(this.configuration);
            var studyMetadata = await studyClient.GetStudyMetadataWithoutUserIdAsync(this.authenticatedUser.TenantId, studyId);

            Console.WriteLine(studyMetadata.AccessInformation.Url);
            
            Console.WriteLine("This command is not yet complete.");
            //var container = new CloudBlobContainer(new Uri(studyMetadata.AccessInformation.)
        }
    }
}
