using System.Threading.Tasks;
using System.Threading;
using Canopy.Api.Client;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public interface IDownloadStudy
    {
        Task<GetStudyQueryResult> ExecuteAsync(string outputFolder, string tenantId, string studyId, int? jobIndex, CancellationToken cancellationToken);
    }
}