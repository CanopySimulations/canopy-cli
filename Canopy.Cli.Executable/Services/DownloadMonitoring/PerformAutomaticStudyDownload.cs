using System.Threading;
using System.IO;
using System.Threading.Tasks;
using Canopy.Cli.Executable.Services.GetStudies;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class PerformAutomaticStudyDownload : IPerformAutomaticStudyDownload
    {
        private readonly IGetStudy getStudy;
        private readonly IMoveCompletedDownloadToken moveCompletedDownloadToken;
        private readonly IAddedDownloadTokensCache addedDownloadTokensCache;

        public PerformAutomaticStudyDownload(
            IGetStudy getStudy,
            IMoveCompletedDownloadToken moveCompletedDownloadToken,
            IAddedDownloadTokensCache addedDownloadTokensCache)
        {
            this.moveCompletedDownloadToken = moveCompletedDownloadToken;
            this.addedDownloadTokensCache = addedDownloadTokensCache;
            this.getStudy = getStudy;
        }

        public async Task ExecuteAsync(
            string tokenPath,
            string outputFolder,
            string tenantId,
            string studyId,
            int? jobIndex,
            bool generateCsv,
            bool keepBinary,
            CancellationToken cancellationToken)
        {
            await this.getStudy.ExecuteAsync(
                new Commands.GetStudyCommand.Parameters(
                    outputFolder,
                    tenantId,
                    studyId,
                    jobIndex,
                    generateCsv,
                    keepBinary),
                cancellationToken);

            this.moveCompletedDownloadToken.Execute(tokenPath, outputFolder);
            this.addedDownloadTokensCache.TryRemove(tokenPath);
        }
    }
}