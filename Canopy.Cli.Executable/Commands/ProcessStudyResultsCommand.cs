using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Canopy.Cli.Shared;
using Canopy.Cli.Shared.StudyProcessing;
using Microsoft.Extensions.CommandLineUtils;

namespace Canopy.Cli.Executable.Commands
{
    using Canopy.Cli.Executable.Helpers;

    public class ProcessStudyResultsCommand : CanopyCommandBase
    {
        private readonly ProcessLocalStudyResults processLocalStudyResults = new ProcessLocalStudyResults();

        private readonly CommandOption targetOption;
        private readonly CommandOption keepOriginalFilesOption;

        public ProcessStudyResultsCommand()
        {
            this.Name = "process-study-results";
            this.Description = "Creates user friendly files from raw study results.";

            this.targetOption = this.Option(
                "-t | --target",
                $"The folder to process. The current directory is used if omitted.",
                CommandOptionType.SingleValue);

            this.keepOriginalFilesOption = this.Option(
                "-ko | --keep-original",
                $"Do not delete files which have been processed (faster).",
                CommandOptionType.NoValue);
        }

        protected override async Task<int> ExecuteAsync()
        {
            var deleteProcessedFiles = !this.keepOriginalFilesOption.HasValue();
            var targetFolder = this.targetOption.Value() ?? Directory.GetCurrentDirectory();

            if (!Directory.Exists(targetFolder))
            {
                Console.WriteLine();
                Console.WriteLine("Folder not found: " + targetFolder);
                return 1;
            }

            await this.processLocalStudyResults.ExecuteAsync(targetFolder, deleteProcessedFiles);

            return 0;
        }
    }
}