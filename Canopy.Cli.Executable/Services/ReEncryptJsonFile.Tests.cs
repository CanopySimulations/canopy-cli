namespace Canopy.Cli.Executable.Services
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Canopy.Cli.Executable.Commands;
    using Canopy.Cli.Executable.Services.DownloadMonitoring;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using Xunit;

    public class ReEncryptJsonFileTests
    {
        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly ISimVersionCache simVersionCache;
        private readonly IContainsEncryptedToken containsEncryptedToken;
        private readonly IReEncryptFile reEncryptFile;
        private readonly IFileOperations fileOperations;
        private readonly ILogger<ReEncryptJsonFile> logger;

        private readonly ReEncryptJsonFile target;

        public ReEncryptJsonFileTests()
        {
            this.ensureAuthenticated = Substitute.For<IEnsureAuthenticated>();;
            this.simVersionCache = Substitute.For<ISimVersionCache>();
            this.containsEncryptedToken = Substitute.For<IContainsEncryptedToken>();
            this.reEncryptFile = Substitute.For<IReEncryptFile>();
            this.fileOperations = Substitute.For<IFileOperations>();
            this.logger = Substitute.For<ILogger<ReEncryptJsonFile>>();

            this.target = new ReEncryptJsonFile(
                this.ensureAuthenticated,
                this.simVersionCache,
                this.containsEncryptedToken,
                this.reEncryptFile,
                this.fileOperations,
                this.logger);
        }
    
        [Fact]
        public async Task WhenFileDoesNotExists_ItShouldThrow()
        {
            var fileInfo = new FileInfo("does-not-exist.json");
            var parameters = new ReEncryptJsonFileCommand.Parameters(
                fileInfo,
                "1.0",
                "tenant");

            this.simVersionCache.GetOrSet("1.0").Returns("2.0");

            this.fileOperations.Exists("does-not-exist.json").Returns(false);

            await Assert.ThrowsAsync<FileNotFoundException>(() => this.target.ExecuteAsync(parameters));

            await this.simVersionCache.Received(1).GetOrSet("1.0");
            await this.ensureAuthenticated.Received(1).ExecuteAsync();
        }
    
        [Fact]
        public async Task WhenEncryptedTokenNotFound_ItShouldDoNothing()
        {
            var fileInfo = new FileInfo("exists.json");
            var parameters = new ReEncryptJsonFileCommand.Parameters(
                fileInfo,
                "1.0",
                "tenant");

            this.simVersionCache.GetOrSet("1.0").Returns("2.0");

            this.fileOperations.Exists(fileInfo.FullName).Returns(true);

            var content = "content";
            this.fileOperations.ReadAllTextAsync(fileInfo.FullName, Arg.Any<CancellationToken>()).Returns(content);
            this.containsEncryptedToken.Execute(content).Returns(false);

            await this.target.ExecuteAsync(parameters);

            await this.reEncryptFile.DidNotReceive().ExecuteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
            await this.fileOperations.DidNotReceive().WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

            await this.simVersionCache.Received(1).GetOrSet("1.0");
            await this.ensureAuthenticated.Received(1).ExecuteAsync();
        }
    
        [Fact]
        public async Task WhenEncryptedTokenFound_ItShouldReEncrypt()
        {
            var fileInfo = new FileInfo("exists.json");
            var parameters = new ReEncryptJsonFileCommand.Parameters(
                fileInfo,
                "1.0",
                "tenant");

            this.simVersionCache.GetOrSet("1.0").Returns("2.0");

            this.fileOperations.Exists(fileInfo.FullName).Returns(true);

            var content = "content";
            this.fileOperations.ReadAllTextAsync(fileInfo.FullName, Arg.Any<CancellationToken>()).Returns(content);
            this.containsEncryptedToken.Execute(content).Returns(true);

            var newContent = "new-content";
            this.reEncryptFile.ExecuteAsync(content, "tenant", "2.0", Arg.Any<CancellationToken>()).Returns(Task.FromResult(newContent));

            await this.target.ExecuteAsync(parameters);

            await this.fileOperations.Received(1).WriteAllTextAsync(fileInfo.FullName, newContent, Arg.Any<CancellationToken>());
            await this.simVersionCache.Received(1).GetOrSet("1.0");
            await this.ensureAuthenticated.Received(1).ExecuteAsync();
        }
    }
}