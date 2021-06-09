using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IReadDownloadToken
    {
        Task<DownloadToken> ExecuteAsync(string filePath, CancellationToken cancellationToken);
    }
}