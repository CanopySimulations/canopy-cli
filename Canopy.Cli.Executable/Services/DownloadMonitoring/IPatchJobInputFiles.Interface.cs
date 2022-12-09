using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IPatchJobInputFiles
    {
        Task ExecuteAsync(
            string folder,
            CancellationToken cancellationToken);
    }
}