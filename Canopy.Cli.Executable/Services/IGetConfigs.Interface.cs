using System.Threading.Tasks;
using Canopy.Cli.Executable.Commands;

namespace Canopy.Cli.Executable.Services
{
    public interface IGetConfigs
    {
        Task ExecuteAsync(GetConfigsCommand.Parameters parameters);
    }
}