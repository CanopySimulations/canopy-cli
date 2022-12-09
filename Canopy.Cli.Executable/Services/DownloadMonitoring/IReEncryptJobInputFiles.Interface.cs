using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IReEncryptJobInputFiles
    {
        Task ExecuteAsync(
            string folder,
            string decryptingTenantShortName,
            StudyDownloadMetadata studyDownloadMetadata,
            CancellationToken cancellationToken);
    }
}