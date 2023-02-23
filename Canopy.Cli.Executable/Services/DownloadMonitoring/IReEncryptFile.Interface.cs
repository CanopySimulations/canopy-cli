using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IReEncryptFile
    {
        Task<string> ExecuteAsync(
            string contents,
            string decryptingTenantShortName,
            string simVersion,
            CancellationToken cancellationToken);
    }
}