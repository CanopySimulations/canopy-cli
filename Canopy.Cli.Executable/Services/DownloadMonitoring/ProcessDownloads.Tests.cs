using System.IO;
using System.Threading;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Canopy.Cli.Shared;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class ProcessDownloadsTests
    {
        private readonly IGetDownloadTokenFolderName getDownloadTokenFolderName;
        private readonly IPerformAutomaticStudyDownload performAutomaticStudyDownload;
        private readonly IGetAvailableOutputFolder getAvailableOutputFolder;
        private readonly IPostProcessStudyDownload postProcessStudyDownload;
        private readonly ILogger<ProcessDownloads> logger;

        private ProcessDownloads target;

        public ProcessDownloadsTests()
        {
            this.getDownloadTokenFolderName = Substitute.For<IGetDownloadTokenFolderName>();
            this.performAutomaticStudyDownload = Substitute.For<IPerformAutomaticStudyDownload>();
            this.getAvailableOutputFolder = Substitute.For<IGetAvailableOutputFolder>();
            this.postProcessStudyDownload = Substitute.For<IPostProcessStudyDownload>();
            this.logger = Substitute.For<ILogger<ProcessDownloads>>();

            this.target = new(
                this.getDownloadTokenFolderName,
                this.performAutomaticStudyDownload,
                this.getAvailableOutputFolder,
                this.postProcessStudyDownload,
                this.logger);
        }

        [Fact]
        public async Task ItShouldInitiateDownload()
        {
            var token1 = QueuedDownloadToken.Random();
            var token2 = QueuedDownloadToken.Random();
            var channelReader = Channel.CreateUnbounded<QueuedDownloadToken>();
            await channelReader.Writer.WriteAsync(token1);
            await channelReader.Writer.WriteAsync(token2);
            channelReader.Writer.Complete();

            var targetFolder = SingletonRandom.Instance.NextString();
            var generateCsv = SingletonRandom.Instance.NextBoolean();
            var keepBinary = SingletonRandom.Instance.NextBoolean();
            var cancellationToken = new CancellationTokenSource().Token;
            var postProcessorPath = SingletonRandom.Instance.NextString();
            var postProcessorArguments = SingletonRandom.Instance.NextString();

            this.getDownloadTokenFolderName.Execute(token1).Returns("f1");
            this.getDownloadTokenFolderName.Execute(token2).Returns("f2");

            var availableOutputFolder1 = Path.Combine(targetFolder, "f1.1");
            var availableOutputFolder2 = Path.Combine(targetFolder, "f2.1");

            this.getAvailableOutputFolder.Execute(Path.Combine(targetFolder, "f1")).Returns(availableOutputFolder1);
            this.getAvailableOutputFolder.Execute(Path.Combine(targetFolder, "f2")).Returns(availableOutputFolder2);

            this.performAutomaticStudyDownload.ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            this.postProcessStudyDownload.ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()).Returns(Task.CompletedTask);

            await this.target.ExecuteAsync(
                channelReader,
                targetFolder,
                generateCsv,
                keepBinary,
                postProcessorPath,
                postProcessorArguments,
                cancellationToken);

            await this.performAutomaticStudyDownload.Received(1).ExecuteAsync(
                token1.TokenPath,
                availableOutputFolder1,
                token1.Token.TenantId,
                token1.Token.StudyId,
                token1.Token.Job?.JobIndex,
                generateCsv,
                keepBinary,
                cancellationToken);

            await this.performAutomaticStudyDownload.Received(1).ExecuteAsync(
                token2.TokenPath,
                availableOutputFolder2,
                token2.Token.TenantId,
                token2.Token.StudyId,
                token2.Token.Job?.JobIndex,
                generateCsv,
                keepBinary,
                cancellationToken);

            await this.postProcessStudyDownload.Received(1).ExecuteAsync(
                postProcessorPath,
                postProcessorArguments,
                availableOutputFolder1);

            await this.postProcessStudyDownload.Received(1).ExecuteAsync(
                postProcessorPath,
                postProcessorArguments,
                availableOutputFolder2);
        }
    }
}