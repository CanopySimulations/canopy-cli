using System.Threading.Tasks;
using Canopy.Api.Client;

namespace Canopy.Cli.Executable.Services
{
    public interface IStudyTypesCache
    {
        Task<GetStudyTypesQueryResult> Get(string tenantId);
        Task<ConfigTypeMetadata> GetConfigTypeMetadata(string tenantId, string configType);
    }
}