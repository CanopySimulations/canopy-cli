using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IPerformAutomaticStudyDownload
    {
        Task ExecuteAsync(
            string tokenPath,
            string outputFolder,
            string tenantId,
            string studyId,
            bool generateCsv,
            bool keepBinary,
            CancellationToken cancellationToken);
    }
}