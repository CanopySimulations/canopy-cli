using System.Threading;
using System.Threading.Tasks;
using Canopy.Cli.Executable.Commands;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public interface IGetStudy
    {
        Task ExecuteAndHandleCancellationAsync(GetStudyCommand.Parameters parameters, CancellationToken cancellationToken);

        Task<GetStudyResult> ExecuteWithResultAsync(GetStudyCommand.Parameters parameters, CancellationToken cancellationToken);
    }
}