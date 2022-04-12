using System.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Canopy.Cli.Executable.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Xunit;
using Canopy.Cli.Executable.Services.GetStudies;

namespace Canopy.Cli.Executable.IntegrationTests
{
    public class GetStudy
    {
        private readonly string StudyName = "Canopy CLI Integration Test Study " + Guid.NewGuid().ToString();

        private readonly ILogger<GetStudy> logger;
        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly ISimVersionCache simVersionCache;
        private readonly IGetDefaultConfig getDefaultConfig;
        private readonly IGetDefaultConfigPath getDefaultConfigPath;
        private readonly ISimVersionClient simVersionClient;
        private readonly IStudyClient studyClient;
        private readonly IWaitForStudy waitForStudy;
        private readonly IGetStudy getStudy;
        private readonly IDownloadBlobDirectoryMock downloadBlobDirectoryMock;

        private string? studyId = null;

        public GetStudy(
            IEnsureAuthenticated ensureAuthenticated,
            ISimVersionCache simVersionCache,
            IGetDefaultConfig getDefaultConfig,
            IGetDefaultConfigPath getDefaultConfigPath,
            ISimVersionClient simVersionClient,
            IStudyClient studyClient,
            IWaitForStudy waitForStudy,
            IGetStudy getStudy,
            IDownloadBlobDirectoryMock downloadBlobDirectoryMock,
            ILogger<GetStudy> logger)
        {
            this.ensureAuthenticated = ensureAuthenticated;
            this.simVersionCache = simVersionCache;
            this.getDefaultConfig = getDefaultConfig;
            this.getDefaultConfigPath = getDefaultConfigPath;
            this.simVersionClient = simVersionClient;
            this.studyClient = studyClient;
            this.waitForStudy = waitForStudy;
            this.getStudy = getStudy;
            this.downloadBlobDirectoryMock = downloadBlobDirectoryMock;
            this.logger = logger;
        }

        public async Task _01_Initialize()
        {
            var authenticationUser = await this.ensureAuthenticated.ExecuteAsync();
            var simVersion = await this.simVersionCache.Get();

            const string WeatherName = "25 deg, dry";
            const string CarName = "Canopy F1 Car 2019";
            
            var loadedCar = await this.getDefaultConfig.ExecuteAsync(authenticationUser.TenantId, simVersion, Constants.CarConfigType, CarName);
            var loadedWeather = await this.getDefaultConfig.ExecuteAsync(authenticationUser.TenantId, simVersion, Constants.WeatherConfigType, WeatherName);

            this.logger.LogInformation("Running StraightSim.");

            var study = new JObject(
                        new JProperty("simConfig", new JObject(
                            new JProperty("car", loadedCar.Content),
                            new JProperty("weather", loadedWeather.Content)
                        )));

            var studyResult = await this.studyClient.PostStudyAsync(
                authenticationUser.TenantId,
                false,
                new NewStudyData
                {
                    Name = StudyName,
                    StudyType = StudyType.StraightSim,
                    Sources = new List<NewStudyDataSource>
                    {
                        new NewStudyDataSource { ConfigType = Constants.CarConfigType, Name = CarName, ConfigId = loadedCar.FilePath },
                        new NewStudyDataSource { ConfigType = Constants.WeatherConfigType, Name = WeatherName, ConfigId = loadedWeather.FilePath },
                    },
                    Study = study,
                    SimVersion = simVersion,
                });

            this.studyId = studyResult.StudyId;

            await this.waitForStudy.ExecuteAsync(authenticationUser.TenantId, this.studyId, TimeSpan.FromMinutes(2));
        }

        public async Task _02_GetStudy()
        {
            var authenticationUser = await this.ensureAuthenticated.ExecuteAsync();
            var simVersion = await this.simVersionCache.Get();
            var outputFolder = new DirectoryInfo("./out");
            var cancellationToken = new CancellationTokenSource().Token;

            Guard.Operation(this.studyId != null, "Study ID was not populated.");

            using (this.downloadBlobDirectoryMock.Record())
            {
                await this.getStudy.ExecuteAsync(new Commands.GetStudyCommand.Parameters(
                    outputFolder.FullName,
                    authenticationUser.TenantId,
                    this.studyId,
                    null,
                    GenerateCsv: false,
                    KeepBinary: false),
                cancellationToken);

                Assert.True(this.downloadBlobDirectoryMock.Count > 0, "No blob folders were downloaded.");
                Assert.True(this.downloadBlobDirectoryMock.Count >= 2, "Not enough blob folders were downloaded.");
            }
        }
    }
}