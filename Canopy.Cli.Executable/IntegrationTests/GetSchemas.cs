using System.IO;
using System.Threading.Tasks;
using Canopy.Cli.Executable.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Canopy.Cli.Executable.IntegrationTests
{
    public class GetSchemas
    {
        private readonly ILogger<GetSchemas> logger;
        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly ISimVersionCache simVersionCache;
        private readonly IGetSchemas getSchemas;
        private readonly IWriteFileMock writeFileMock;

        public GetSchemas(
            IEnsureAuthenticated ensureAuthenticated,
            ISimVersionCache simVersionCache,
            IGetSchemas getSchemas,
            IWriteFileMock writeFileMock,
            ILogger<GetSchemas> logger)
        {
            this.ensureAuthenticated = ensureAuthenticated;
            this.simVersionCache = simVersionCache;
            this.getSchemas = getSchemas;
            this.writeFileMock = writeFileMock;
            this.logger = logger;
        }

        public async Task _01_GetSchemas()
        {
            var authenticationUser = await this.ensureAuthenticated.ExecuteAsync();
            var simVersion = await this.simVersionCache.Get();
            var outputFolder = new DirectoryInfo("./out");

            using (this.writeFileMock.Record())
            {
                await this.getSchemas.ExecuteAsync(new Commands.GetSchemasCommand.Parameters(
                    outputFolder,
                    simVersion,
                    authenticationUser.TenantId));

                Assert.True(this.writeFileMock.Count > 0, "No files were written.");

                var expectedFiles = new[] { "car.schema.json", "weather.schema.json" };
                foreach (var expectedFile in expectedFiles)
                {
                    var expectedPath = Path.Combine(outputFolder.FullName, expectedFile);
                    Assert.True(this.writeFileMock.GetSize(expectedPath) > 0, $"Zero file size written for config {expectedFile}");
                }
            }
        }
    }
}