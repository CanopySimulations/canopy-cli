using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Canopy.Cli.Executable.Commands;
using Canopy.Cli.Shared;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class RunDownloaderTests
    {
        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly IGetCreatedOutputFolder getCreatedOutputFolder;
        private readonly IMonitorDownloads monitorDownloads;
        private readonly IProcessDownloads processDownloads;
        private readonly ILogger<RunDownloader> logger;

        private RunDownloader target;

        public RunDownloaderTests()
        {
            this.ensureAuthenticated = Substitute.For<IEnsureAuthenticated>();
            this.getCreatedOutputFolder = Substitute.For<IGetCreatedOutputFolder>();
            this.monitorDownloads = Substitute.For<IMonitorDownloads>();
            this.processDownloads = Substitute.For<IProcessDownloads>();
            this.logger = Substitute.For<ILogger<RunDownloader>>();

            this.target = new (
                this.ensureAuthenticated,
                this.getCreatedOutputFolder,
                this.monitorDownloads,
                this.processDownloads,
                this.logger);
        }

        [Fact]
        public async Task ItShouldRunTheDownloader()
        {
            var parameters = DownloadMonitorCommand.Parameters.Random();

            this.ensureAuthenticated.ExecuteAsync().Returns(Task.FromResult(AuthenticatedUser.Random()));

            var createdInputFolder = SingletonRandom.Instance.NextString();
            this.getCreatedOutputFolder.Execute(parameters.InputFolder).Returns(createdInputFolder);

            var createdOutputFolder = SingletonRandom.Instance.NextString();
            this.getCreatedOutputFolder.Execute(parameters.OutputFolder).Returns(createdOutputFolder);

            this.monitorDownloads.ExecuteAsync(
                Arg.Any<ChannelWriter<QueuedDownloadToken>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            this.processDownloads.ExecuteAsync(
                Arg.Any<ChannelReader<QueuedDownloadToken>>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<PostProcessingParameters>(),
                Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            await this.target.ExecuteAsync(parameters);
            
            await this.monitorDownloads.Received(1).ExecuteAsync(
                Arg.Any<ChannelWriter<QueuedDownloadToken>>(),
                createdInputFolder,
                Arg.Any<CancellationToken>());

            await this.processDownloads.Received(1).ExecuteAsync(
                Arg.Any<ChannelReader<QueuedDownloadToken>>(),
                createdOutputFolder,
                parameters.GenerateCsv,
                parameters.KeepBinary,
                new PostProcessingParameters(
                    parameters.PostProcessor,
                    parameters.PostProcessorArguments,
                    parameters.DecryptingTenantShortName),
                Arg.Any<CancellationToken>());
        }
    }
}