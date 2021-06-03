using System;
using System.IO;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Canopy.Cli.Executable.IntegrationTestsRunner;
using Canopy.Cli.Executable.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Canopy.Cli.Executable.IntegrationTests
{
    public class GetConfigs
    {
        private readonly IGetConfigs getConfigs;
        private readonly IWriteFileMock writeFileMock;
        private readonly ILogger<GetConfigs> logger;
        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly IGetDefaultConfigPath getDefaultConfigPath;
        private readonly ISimVersionCache simVersionCache;
        private readonly ISimVersionClient simVersionClient;
        private readonly IConfigClient configClient;

        private readonly string WeatherConfigName = "Canopy CLI Integration Test Weather " + Guid.NewGuid().ToString();

        public GetConfigs(
            IEnsureAuthenticated ensureAuthenticated,
            IGetDefaultConfigPath getDefaultConfigPath,
            ISimVersionCache simVersionCache,
            ISimVersionClient simVersionClient,
            IConfigClient configClient,
            IGetConfigs getConfigs,
            IWriteFileMock writeFileMock,
            ILogger<GetConfigs> logger)
        {
            this.ensureAuthenticated = ensureAuthenticated;
            this.getDefaultConfigPath = getDefaultConfigPath;
            this.simVersionCache = simVersionCache;
            this.simVersionClient = simVersionClient;
            this.configClient = configClient;
            this.getConfigs = getConfigs;
            this.writeFileMock = writeFileMock;
            this.logger = logger;
        }

        public async Task _01_Initialize()
        {
            var authenticationUser = await this.ensureAuthenticated.ExecuteAsync();
            var simVersion = await this.simVersionCache.Get();
            var weatherConfigPath = await this.getDefaultConfigPath.Execute(authenticationUser.TenantId, Constants.WeatherConfigType, "25 deg, dry");
            
            this.logger.LogInformation("Fetching default weather config.");
            var weatherDocument = await this.simVersionClient.GetDocumentAsync(
                simVersion,
                weatherConfigPath,
                authenticationUser.TenantId);
            var weatherContent = JObject.Parse(weatherDocument.Document.Content);
            
            this.logger.LogInformation("Saving as user weather config.");
            var configId = await this.configClient.PostConfigAsync(
                authenticationUser.TenantId,
                new NewConfigData
                {
                    Name = WeatherConfigName,
                    ConfigType = Constants.WeatherConfigType,
                    Config = weatherContent,
                    SimVersion = simVersion,
                },
                null);
        }

        public async Task _02_GetWeatherConfigs()
        {
            var authenticationUser = await this.ensureAuthenticated.ExecuteAsync();
            var simVersion = await this.simVersionCache.Get();
            var outputFolder = new DirectoryInfo("./out");

            using (this.writeFileMock.Record())
            {
                await this.getConfigs.ExecuteAsync(new Commands.GetConfigsCommand.Parameters(
                    Constants.WeatherConfigType,
                    authenticationUser.UserId,
                    Username: string.Empty,
                    outputFolder,
                    simVersion,
                    Unwrap: true,
                    Format: false));

                Assert.True(this.writeFileMock.Count > 0, "No files were written.");

                var fileName = WeatherConfigName + ".json";
                var expectedPath = Path.Combine(outputFolder.FullName, fileName);

                Assert.True(this.writeFileMock.GetSize(expectedPath) > 0, $"Zero file size written for config {fileName}");
            }
        }
    }
}