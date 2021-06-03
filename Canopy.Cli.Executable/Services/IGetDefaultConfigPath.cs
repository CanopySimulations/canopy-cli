using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public interface IGetDefaultConfigPath
    {
        Task<string> Execute(string tenantId, string configType, string documentName);
    }
}