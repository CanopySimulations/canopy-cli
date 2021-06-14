using System.Threading.Tasks;
using Canopy.Cli.Executable.Commands;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IRunDownloader
    {
        Task ExecuteAsync(DownloadMonitorCommand.Parameters parameters);
    }
}