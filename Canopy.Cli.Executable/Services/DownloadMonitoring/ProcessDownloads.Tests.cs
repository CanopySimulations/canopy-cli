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
        private readonly ILogger<ProcessDownloads> logger;

        private ProcessDownloads target;

        public ProcessDownloadsTests()
        {
            this.getDownloadTokenFolderName = Substitute.For<IGetDownloadTokenFolderName>();
            this.performAutomaticStudyDownload = Substitute.For<IPerformAutomaticStudyDownload>();
            this.getAvailableOutputFolder = Substitute.For<IGetAvailableOutputFolder>();
            this.logger = Substitute.For<ILogger<ProcessDownloads>>();

            this.target = new(
                this.getDownloadTokenFolderName,
                this.performAutomaticStudyDownload,
                this.getAvailableOutputFolder,
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

            this.getDownloadTokenFolderName.Execute(token1).Returns("f1");
            this.getDownloadTokenFolderName.Execute(token2).Returns("f2");

            this.getAvailableOutputFolder.Execute(Path.Combine(targetFolder, "f1")).Returns(Path.Combine(targetFolder, "f1.1"));
            this.getAvailableOutputFolder.Execute(Path.Combine(targetFolder, "f2")).Returns(Path.Combine(targetFolder, "f2.1"));

            this.performAutomaticStudyDownload.ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<bool>()).Returns(Task.CompletedTask);

            await this.target.ExecuteAsync(
                channelReader,
                targetFolder,
                generateCsv,
                keepBinary,
                cancellationToken);

            await this.performAutomaticStudyDownload.Received(1).ExecuteAsync(
                token1.TokenPath,
                Path.Combine(targetFolder, "f1.1"),
                token1.Token.TenantId,
                token1.Token.StudyId,
                generateCsv,
                keepBinary);

            await this.performAutomaticStudyDownload.Received(1).ExecuteAsync(
                token2.TokenPath,
                Path.Combine(targetFolder, "f2.1"),
                token2.Token.TenantId,
                token2.Token.StudyId,
                generateCsv,
                keepBinary);
        }
    }
}