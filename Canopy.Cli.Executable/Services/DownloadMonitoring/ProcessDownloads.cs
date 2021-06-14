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
        private readonly IPostProcessStudyDownload postProcessStudyDownload;
        private readonly ILogger<ProcessDownloads> logger;

        public ProcessDownloads(
            IGetDownloadTokenFolderName getDownloadTokenFolderName,
            IPerformAutomaticStudyDownload performAutomaticStudyDownload,
            IGetAvailableOutputFolder getAvailableOutputFolder,
            IPostProcessStudyDownload postProcessStudyDownload,
            ILogger<ProcessDownloads> logger)
        {
            this.getAvailableOutputFolder = getAvailableOutputFolder;
            this.postProcessStudyDownload = postProcessStudyDownload;
            this.logger = logger;
            this.getDownloadTokenFolderName = getDownloadTokenFolderName;
            this.performAutomaticStudyDownload = performAutomaticStudyDownload;
        }

        public async Task ExecuteAsync(
            ChannelReader<QueuedDownloadToken> channelReader,
            string targetFolder,
            bool generateCsv,
            bool keepBinary,
            string postProcessorPath,
            string postProcessorArguments,
            CancellationToken cancellationToken)
        {
            await foreach (var item in channelReader.ReadAllAsync(cancellationToken))
            {
                string folderName = this.getDownloadTokenFolderName.Execute(item);

                var outputFolder = Path.Combine(targetFolder, folderName);

                outputFolder = this.getAvailableOutputFolder.Execute(outputFolder);

                this.logger.LogInformation("Starting download of {0} to {1}.", folderName, outputFolder);

                await this.performAutomaticStudyDownload.ExecuteAsync(
                    tokenPath: item.TokenPath,
                    outputFolder: outputFolder,
                    tenantId: item.Token.TenantId,
                    studyId: item.Token.StudyId,
                    generateCsv: generateCsv,
                    keepBinary: keepBinary,
                    cancellationToken: cancellationToken);

                this.logger.LogInformation("Completed download of {0} to {1}.", folderName, outputFolder);

                if (!string.IsNullOrWhiteSpace(postProcessorPath))
                {
                    this.logger.LogInformation("Running post-processor on {0}.", outputFolder);

                    await this.postProcessStudyDownload.ExecuteAsync(
                        postProcessorPath,
                        postProcessorArguments,
                        outputFolder);
                    
                    this.logger.LogInformation("Completed running post-processor on {0}.", outputFolder);
                }
            }
        }
    }
}