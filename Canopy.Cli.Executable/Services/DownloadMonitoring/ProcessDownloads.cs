using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class ProcessDownloads : IProcessDownloads
    {
        private readonly IGetDownloadTokenFolderName getDownloadTokenFolderName;
        private readonly IPerformAutomaticStudyDownload performAutomaticStudyDownload;
        private readonly IGetAvailableOutputFolder getAvailableOutputFolder;
        private readonly IRunAllPostProcessors runAllPostProcessors;
        private readonly ILogger<ProcessDownloads> logger;

        public ProcessDownloads(
            IGetDownloadTokenFolderName getDownloadTokenFolderName,
            IPerformAutomaticStudyDownload performAutomaticStudyDownload,
            IGetAvailableOutputFolder getAvailableOutputFolder,
            IRunAllPostProcessors runAllPostProcessors,
            ILogger<ProcessDownloads> logger)
        {
            this.getAvailableOutputFolder = getAvailableOutputFolder;
            this.runAllPostProcessors = runAllPostProcessors;
            this.logger = logger;
            this.getDownloadTokenFolderName = getDownloadTokenFolderName;
            this.performAutomaticStudyDownload = performAutomaticStudyDownload;
        }

        public async Task ExecuteAsync(
            ChannelReader<QueuedDownloadToken> channelReader,
            string targetFolder,
            bool generateCsv,
            bool keepBinary,
            PostProcessingParameters postProcessingParameters,
            CancellationToken cancellationToken)
        {
            await foreach (var item in channelReader.ReadAllAsync(cancellationToken))
            {
                string folderName = this.getDownloadTokenFolderName.Execute(item);

                var outputFolder = Path.Combine(targetFolder, folderName);

                outputFolder = this.getAvailableOutputFolder.Execute(outputFolder);

                this.logger.LogInformation("Starting download of {0} to {1}.", folderName, outputFolder);

                var studyDownloadMetadata = await this.performAutomaticStudyDownload.ExecuteAsync(
                    tokenPath: item.TokenPath,
                    outputFolder: outputFolder,
                    tenantId: item.Token.TenantId,
                    studyId: item.Token.StudyId,
                    jobIndex: item.Token.Job?.JobIndex,
                    generateCsv: generateCsv,
                    keepBinary: keepBinary,
                    cancellationToken: cancellationToken);

                this.logger.LogInformation("Completed download of {0} to {1}.", folderName, outputFolder);

                await this.runAllPostProcessors.ExecuteAsync(
                    postProcessingParameters,
                    outputFolder,
                    studyDownloadMetadata,
                    cancellationToken);
            }
        }
    }
}