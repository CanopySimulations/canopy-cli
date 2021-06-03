using System.Threading.Tasks;
using Canopy.Api.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Canopy.Cli.Executable.Services
{
    public record DefaultConfigResult(string FilePath, JObject Content);
    
    public class GetDefaultConfig : IGetDefaultConfig
    {
        private readonly IGetDefaultConfigPath getDefaultConfigPath;
        private readonly ISimVersionClient simVersionClient;
        private readonly ILogger<GetDefaultConfig> logger;

        public GetDefaultConfig(
            IGetDefaultConfigPath getDefaultConfigPath,
            ISimVersionClient simVersionClient,
            ILogger<GetDefaultConfig> logger)
        {
            this.getDefaultConfigPath = getDefaultConfigPath;
            this.simVersionClient = simVersionClient;
            this.logger = logger;
        }

        public async Task<DefaultConfigResult> ExecuteAsync(string tenantId, string simVersion, string configType, string name)
        {
            var configPath = await this.getDefaultConfigPath.Execute(tenantId, configType, name);

            this.logger.LogInformation("Fetching default {0} config {1}.", configType, name);
            var document = await this.simVersionClient.GetDocumentAsync(
                simVersion,
                configPath,
                tenantId);

            var content = JObject.Parse(document.Document.Content);

            return new DefaultConfigResult(configPath, content);
        }
    }
}