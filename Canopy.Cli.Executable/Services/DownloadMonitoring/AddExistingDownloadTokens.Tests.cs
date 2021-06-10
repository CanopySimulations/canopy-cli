using System.Threading;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Canopy.Cli.Shared;
using NSubstitute;
using Xunit;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class AddExistingDownloadTokensTests
    {
        private static readonly string FolderPath = SingletonRandom.Instance.NextString();
        private static readonly string TokenPath1 = SingletonRandom.Instance.NextString();
        private static readonly string TokenPath2 = SingletonRandom.Instance.NextString();

        private readonly IGetDownloadTokens getDownloadTokens;
        private readonly ITryAddDownloadToken tryAddDownloadToken;
        private readonly ILogger<AddExistingDownloadTokens> logger;

        private readonly AddExistingDownloadTokens target;

        public AddExistingDownloadTokensTests()
        {
            this.getDownloadTokens = Substitute.For<IGetDownloadTokens>();
            this.tryAddDownloadToken = Substitute.For<ITryAddDownloadToken>();
            this.logger = Substitute.For<ILogger<AddExistingDownloadTokens>>();

            this.target = new (
                this.getDownloadTokens,
                this.tryAddDownloadToken,
                this.logger);
        } 

        [Fact]
        public async Task ItShouldAddAllExistingTokens()
        {
            this.getDownloadTokens.Execute(FolderPath).Returns(new []
            {
                TokenPath1,
                TokenPath2,
            });

            var channel = Channel.CreateUnbounded<QueuedDownloadToken>();
            var channelWriter = channel.Writer;

            var cancellationToken = new CancellationTokenSource().Token;

            this.tryAddDownloadToken.ExecuteAsync(channelWriter, Arg.Any<string>(), cancellationToken).Returns(Task.CompletedTask);

            await this.target.ExecuteAsync(channelWriter, FolderPath, cancellationToken);

            await this.tryAddDownloadToken.Received(2).ExecuteAsync(channelWriter, Arg.Any<string>(), cancellationToken);
            await this.tryAddDownloadToken.Received(1).ExecuteAsync(channelWriter, TokenPath1, cancellationToken);
            await this.tryAddDownloadToken.Received(1).ExecuteAsync(channelWriter, TokenPath2, cancellationToken);
        }
    }
}