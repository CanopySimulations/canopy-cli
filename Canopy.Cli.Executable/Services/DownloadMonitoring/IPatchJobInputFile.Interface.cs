using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IPatchJobInputFile
    {
        Task ExecuteAsync(
            string studyBaselineText,
            string patchFilePath,
            CancellationToken cancellationToken);
    }
}