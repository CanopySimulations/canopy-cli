using System.Threading.Tasks;
using Canopy.Cli.Executable.Commands;

namespace Canopy.Cli.Executable.Services
{
    public interface IRunDownloader
    {
        Task ExecuteAsync(DownloaderCommand.Parameters parameters);
    }
}