using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IReEncryptFile
    {
        Task<string> ExecuteAsync(
            string contents,
            string decryptingTenantShortName,
            StudyDownloadMetadata studyDownloadMetadata,
            CancellationToken cancellationToken);
    }
}