using System.Threading.Tasks;
using Canopy.Cli.Executable.Commands;

namespace Canopy.Cli.Executable.Services
{
    public interface IGetSchemas
    {
        Task ExecuteAsync(GetSchemasCommand.Parameters parameters);
    }
}