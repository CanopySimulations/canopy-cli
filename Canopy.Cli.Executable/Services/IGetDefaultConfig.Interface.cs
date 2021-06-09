using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Canopy.Cli.Executable.Services
{
    public interface IGetDefaultConfig
    {
        Task<DefaultConfigResult> ExecuteAsync(string tenantId, string simVersion, string configType, string name);
    }
}