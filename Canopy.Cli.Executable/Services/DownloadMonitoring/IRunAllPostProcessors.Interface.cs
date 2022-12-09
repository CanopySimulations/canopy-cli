using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IRunAllPostProcessors
    {
        Task ExecuteAsync(
            PostProcessingParameters parameters,
            string folder,
            StudyDownloadMetadata studyDownloadMetadata,
            CancellationToken cancellationToken);
    }
}