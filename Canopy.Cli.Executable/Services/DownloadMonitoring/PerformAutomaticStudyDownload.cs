using System.IO;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class PerformAutomaticStudyDownload : IPerformAutomaticStudyDownload
    {
        private readonly IGetStudy getStudy;
        private readonly IMoveCompletedDownloadToken moveCompletedDownloadToken;

        public PerformAutomaticStudyDownload(
            IGetStudy getStudy,
            IMoveCompletedDownloadToken moveCompletedDownloadToken)
        {
            this.moveCompletedDownloadToken = moveCompletedDownloadToken;
            this.getStudy = getStudy;
        }

        public async Task ExecuteAsync(
            string tokenPath,
            string outputFolder,
            string tenantId,
            string studyId,
            bool generateCsv,
            bool keepBinary)
        {
            await this.getStudy.ExecuteAsync(
                new Commands.GetStudyCommand.Parameters(
                    outputFolder,
                    tenantId,
                    studyId,
                    generateCsv,
                    keepBinary));

            this.moveCompletedDownloadToken.Execute(tokenPath, outputFolder);
        }
    }
}