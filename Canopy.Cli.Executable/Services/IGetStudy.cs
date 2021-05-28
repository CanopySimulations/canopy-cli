using System.Threading.Tasks;
using Canopy.Cli.Executable.Commands;

namespace Canopy.Cli.Executable.Services
{
    public interface IGetStudy
    {
        Task ExecuteAsync(GetStudyCommand.Parameters parameters);
    }
}