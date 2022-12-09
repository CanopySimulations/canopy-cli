using System.Threading;
using System.Threading.Tasks;
using Canopy.Cli.Executable.Services.GetStudies;
using Canopy.Cli.Shared;
using NSubstitute;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class PerformAutomaticStudyDownloadTests
    {
        private readonly IGetStudy getStudy;
        private readonly IMoveCompletedDownloadToken moveCompletedDownloadToken;
        private readonly IAddedDownloadTokensCache addedDownloadTokensCache;

        private readonly PerformAutomaticStudyDownload target;

        public PerformAutomaticStudyDownloadTests()
        {
            this.getStudy = Substitute.For<IGetStudy>();
            this.moveCompletedDownloadToken = Substitute.For<IMoveCompletedDownloadToken>();
            this.addedDownloadTokensCache = Substitute.For<IAddedDownloadTokensCache>();

            this.target = new PerformAutomaticStudyDownload(
                this.getStudy,
                this.moveCompletedDownloadToken,
                this.addedDownloadTokensCache);
        }

        [Fact]
        public async Task ItShouldOrchestrateDownloadingStudy()
        {
            var parameters = Commands.GetStudyCommand.Parameters.Random();
            var tokenPath = SingletonRandom.Instance.NextString();
            var cancellationToken = new CancellationTokenSource().Token;

            var getStudyResult = GetStudyResult.Random();

            this.getStudy.ExecuteWithResultAsync(Arg.Any<Commands.GetStudyCommand.Parameters>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(getStudyResult));

            var result = await this.target.ExecuteAsync(
                tokenPath,
                parameters.OutputFolder,
                parameters.TenantId,
                parameters.StudyId,
                parameters.JobIndex,
                parameters.GenerateCsv,
                parameters.KeepBinary,
                cancellationToken);

            await this.getStudy.Received(1).ExecuteWithResultAsync(parameters, cancellationToken);

            this.moveCompletedDownloadToken.Received(1).Execute(tokenPath, parameters.OutputFolder);
            this.addedDownloadTokensCache.Received(1).TryRemove(tokenPath);

            Assert.Equal(
                new StudyDownloadMetadata(getStudyResult.SimVersion),
                result);
        }
    }
}