using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public class GetDefaultConfigPath : IGetDefaultConfigPath
    {
        private readonly IStudyTypesCache studyTypesCache;

        public GetDefaultConfigPath(
            IStudyTypesCache studyTypesCache)
        {
            this.studyTypesCache = studyTypesCache;
        }

        public async Task<string> Execute(string tenantId, string configType, string documentName)
        {
            var metadata = await this.studyTypesCache.GetConfigTypeMetadata(tenantId, configType);

            return $"{metadata.PluralKey}/{documentName}.json";
        }
    }
}