using System.Threading;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using Canopy.Cli.Shared;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using System.Collections.Generic;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class WatchForNewDownloadTokensTests
    {
        private readonly ITryAddDownloadToken tryAddDownloadToken;
        private readonly ILogger<WatchForNewDownloadTokens> logger;

        private readonly WatchForNewDownloadTokens target;

        public WatchForNewDownloadTokensTests()
        {
            this.tryAddDownloadToken = Substitute.For<ITryAddDownloadToken>();
            this.logger = Substitute.For<ILogger<WatchForNewDownloadTokens>>();

            this.target = new(
                this.tryAddDownloadToken,
                this.logger);
        }

        [Fact]
        public async Task It()
        {
            const int delayMs = 10;

            using var tempFolder = TestUtilities.GetTempFolder();

            var channel = Channel.CreateUnbounded<QueuedDownloadToken>();
            var cts = new CancellationTokenSource();

            this.tryAddDownloadToken.ExecuteAsync(channel.Writer, Arg.Any<string>(), cts.Token).Returns(Task.CompletedTask);

            var file1 = await this.WriteFile(tempFolder.Path, "a" + DownloaderConstants.DownloadTokenExtensionWithPeriod);

            var task = this.target.ExecuteAsync(channel.Writer, tempFolder.Path, cts.Token);

            Assert.False(task.IsCompleted);

            var file2 = await this.WriteFile(tempFolder.Path, "b" + DownloaderConstants.DownloadTokenExtensionWithPeriod);
            var file3 = await this.WriteFile(tempFolder.Path, "c.other");
            var file4 = await this.WriteFile(tempFolder.Path, DownloaderConstants.CompletedDownloadTokenFileName);
            var file5 = await this.WriteFile(tempFolder.Path, "d" + DownloaderConstants.DownloadTokenExtensionWithPeriod);

            await Task.Delay(delayMs);

            Assert.False(task.IsCompleted);

            cts.Cancel();

            await Task.Delay(delayMs);

            Assert.True(task.IsCompleted);

            await this.tryAddDownloadToken.Received(0).ExecuteAsync(channel.Writer, file1, cts.Token);
            await this.tryAddDownloadToken.Received(1).ExecuteAsync(channel.Writer, file2, cts.Token);
            await this.tryAddDownloadToken.Received(0).ExecuteAsync(channel.Writer, file3, cts.Token);
            await this.tryAddDownloadToken.Received(1).ExecuteAsync(channel.Writer, file4, cts.Token);
            await this.tryAddDownloadToken.Received(1).ExecuteAsync(channel.Writer, file5, cts.Token);
        }

        private async Task<string> WriteFile(string folderPath, string fileName)
        {
            var filePath = Path.Combine(folderPath, fileName);
            await File.WriteAllTextAsync(filePath, SingletonRandom.Instance.NextString());
            return filePath;
        }
    }
}