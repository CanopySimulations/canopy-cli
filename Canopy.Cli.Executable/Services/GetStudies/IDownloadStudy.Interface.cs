using System.Threading.Tasks;
using System.Threading;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public interface IDownloadStudy
    {
        Task ExecuteAsync(string outputFolder, string tenantId, string studyId, int? jobIndex, CancellationToken cancellationToken);
    }
}