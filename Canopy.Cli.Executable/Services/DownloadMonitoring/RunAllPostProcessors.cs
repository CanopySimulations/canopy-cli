using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class RunAllPostProcessors : IRunAllPostProcessors
    {
        private readonly IPatchJobInputFiles patchJobInputFiles;
        private readonly IReEncryptJobInputFiles reEncryptJobInputFiles;
        private readonly IPostProcessStudyDownload postProcessStudyDownload;
        private readonly ILogger<RunAllPostProcessors> logger;

        public RunAllPostProcessors(
            IPatchJobInputFiles patchJobInputFiles,
            IReEncryptJobInputFiles reEncryptJobInputFiles,
            IPostProcessStudyDownload postProcessStudyDownload,
            ILogger<RunAllPostProcessors> logger)
        {
            this.postProcessStudyDownload = postProcessStudyDownload;
            this.logger = logger;
            this.patchJobInputFiles = patchJobInputFiles;
            this.reEncryptJobInputFiles = reEncryptJobInputFiles;
        }

        public async Task ExecuteAsync(
            PostProcessingParameters parameters,
            string folder,
            StudyDownloadMetadata studyDownloadMetadata,
            CancellationToken cancellationToken)
        {
            await this.patchJobInputFiles.ExecuteAsync(folder, cancellationToken);

            if (!string.IsNullOrWhiteSpace(parameters.DecryptingTenantShortName))
            {
                await this.reEncryptJobInputFiles.ExecuteAsync(
                    folder,
                    parameters.DecryptingTenantShortName,
                    studyDownloadMetadata,
                    cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(parameters.PostProcessorPath))
            {
                this.logger.LogInformation("Running post-processor on {0}.", folder);

                await this.postProcessStudyDownload.ExecuteAsync(
                    parameters.PostProcessorPath,
                    parameters.PostProcessorArguments,
                    folder);

                this.logger.LogInformation("Completed running post-processor on {0}.", folder);
            }
        }
    }
}