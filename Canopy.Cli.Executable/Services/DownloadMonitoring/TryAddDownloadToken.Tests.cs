using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Canopy.Cli.Shared;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class TryAddDownloadTokenTests
    {
        private readonly IAddedDownloadTokensCache addedDownloadTokensCache;
        private readonly IReadDownloadToken readDownloadToken;
        private readonly ILogger<TryAddDownloadToken> logger;

        private readonly TryAddDownloadToken target;

        public TryAddDownloadTokenTests()
        {
            this.addedDownloadTokensCache = Substitute.For<IAddedDownloadTokensCache>();
            this.readDownloadToken = Substitute.For<IReadDownloadToken>();
            this.logger = Substitute.For<ILogger<TryAddDownloadToken>>();

            this.target = new(
                this.addedDownloadTokensCache,
                this.readDownloadToken,
                this.logger);
        }

        [Fact]
        public async Task WhenTheTokenHasNotBeenAddedItShouldWriteToTheChannel()
        {
            var channel = Channel.CreateUnbounded<QueuedDownloadToken>();
            var queuedToken = QueuedDownloadToken.Random();
            var cancellationToken = new CancellationTokenSource().Token;
            var filePath = queuedToken.TokenPath;

            this.addedDownloadTokensCache.TryAdd(filePath).Returns(true);

            this.readDownloadToken.ExecuteAsync(filePath, cancellationToken).Returns(Task.FromResult(queuedToken.Token));

            await this.target.ExecuteAsync(channel.Writer, filePath, cancellationToken);

            channel.Writer.Complete();

            var writtenTokens = new List<QueuedDownloadToken>();
            await foreach(var item in channel.Reader.ReadAllAsync())
            {
                writtenTokens.Add(item);
            }

            Assert.Single(writtenTokens);
            Assert.Equal(queuedToken, writtenTokens.Single());
        }
    }
}