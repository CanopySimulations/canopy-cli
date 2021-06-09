using System.Threading.Tasks;
using Canopy.Cli.Shared;
using NSubstitute;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class PerformAutomaticStudyDownloadTests
    {
        private readonly IGetStudy getStudy;
        private readonly IMoveCompletedDownloadToken moveCompletedDownloadToken;

        private readonly PerformAutomaticStudyDownload target;

        public PerformAutomaticStudyDownloadTests()
        {
            this.getStudy = Substitute.For<IGetStudy>();
            this.moveCompletedDownloadToken = Substitute.For<IMoveCompletedDownloadToken>();

            this.target = new PerformAutomaticStudyDownload(
                this.getStudy,
                this.moveCompletedDownloadToken);
        }

        [Fact]
        public async Task ItShouldOrchestrateDownloadingStudy()
        {
            var parameters = Commands.GetStudyCommand.Parameters.Random();
            var tokenPath = SingletonRandom.Instance.NextString();

            this.getStudy.ExecuteAsync(Arg.Any<Commands.GetStudyCommand.Parameters>()).Returns(Task.CompletedTask);

            await this.target.ExecuteAsync(
                tokenPath,
                parameters.OutputFolder,
                parameters.TenantId,
                parameters.StudyId,
                parameters.GenerateCsv,
                parameters.KeepBinary);

            await this.getStudy.Received(1).ExecuteAsync(parameters);

            this.moveCompletedDownloadToken.Received(1).Execute(tokenPath, parameters.OutputFolder);
        }
    }
}