using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IPostProcessStudyDownload
    {
        Task ExecuteAsync(
            string postProcessorPath,
            string postProcessorArguments,
            string targetFolder);
    }
}