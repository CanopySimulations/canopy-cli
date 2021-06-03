using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Canopy.Api.Client;

namespace Canopy.Cli.Executable.Services
{
    public class StudyTypesCache : IStudyTypesCache
    {
        private readonly IStudyClient studyClient;

        private readonly ConcurrentDictionary<string, Task<GetStudyTypesQueryResult>> cache = new();

        public StudyTypesCache(
            IStudyClient studyClient)
        {
            this.studyClient = studyClient;
        }

        public async Task<ConfigTypeMetadata> GetConfigTypeMetadata(string tenantId, string configType)
        {
            var studyTypesResult = await this.Get(tenantId);

            var result = studyTypesResult.ConfigTypeMetadata.FirstOrDefault(v => v.SingularKey == configType);
            if (result == null)
            {
                throw new RecoverableException($"Unknown config type: {configType}");
            }

            return result;
        }

        public Task<GetStudyTypesQueryResult> Get(string tenantId)
        {
            return this.cache.GetOrAdd(tenantId, v => this.GetInner(v));
        }

        private Task<GetStudyTypesQueryResult> GetInner(string tenantId)
        {
            return this.studyClient.GetStudyTypesAsync(tenantId);
        }
    }
}