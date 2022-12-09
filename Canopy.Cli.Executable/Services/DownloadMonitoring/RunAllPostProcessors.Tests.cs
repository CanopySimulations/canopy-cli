using System.Threading;
using System.Threading.Tasks;
using Canopy.Cli.Shared;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class RunAllPostProcessorsTests
    {
        private readonly IPatchJobInputFiles patchJobInputFiles;
        private readonly IReEncryptJobInputFiles reEncryptJobInputFiles;
        private readonly IPostProcessStudyDownload postProcessStudyDownload;
        private readonly ILogger<RunAllPostProcessors> logger;

        private readonly RunAllPostProcessors target;

        public RunAllPostProcessorsTests()
        {
            this.patchJobInputFiles = Substitute.For<IPatchJobInputFiles>();
            this.reEncryptJobInputFiles = Substitute.For<IReEncryptJobInputFiles>();
            this.postProcessStudyDownload = Substitute.For<IPostProcessStudyDownload>();
            this.logger = Substitute.For<ILogger<RunAllPostProcessors>>();

            this.target = new RunAllPostProcessors(
                this.patchJobInputFiles,
                this.reEncryptJobInputFiles,
                this.postProcessStudyDownload,
                this.logger);
        }

        [Fact]
        public async Task ItShouldPatchJobInputFiles()
        {
            var postProcessingParameters = new PostProcessingParameters(string.Empty, string.Empty, string.Empty);
            var folder = SingletonRandom.Instance.NextString();
            var studyDownloadMetadata = StudyDownloadMetadata.Random();
            var cancellationToken = new CancellationTokenSource().Token;
            
            this.patchJobInputFiles.ExecuteAsync(folder, cancellationToken).Returns(Task.CompletedTask);

            await this.target.ExecuteAsync(
                postProcessingParameters,
                folder,
                studyDownloadMetadata,
                cancellationToken);
            
            await this.patchJobInputFiles.Received(1).ExecuteAsync(folder, cancellationToken);

            await this.reEncryptJobInputFiles.DidNotReceive().ExecuteAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<StudyDownloadMetadata>(), cancellationToken);

            await this.postProcessStudyDownload.DidNotReceive().ExecuteAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task ItShouldAlsoReEncryptIfDecryptingTenantShortNameSupplied()
        {
            var postProcessingParameters = new PostProcessingParameters(string.Empty, string.Empty, "decrypting-tsn");
            var folder = SingletonRandom.Instance.NextString();
            var studyDownloadMetadata = StudyDownloadMetadata.Random();
            var cancellationToken = new CancellationTokenSource().Token;
            
            this.patchJobInputFiles.ExecuteAsync(folder, cancellationToken).Returns(Task.CompletedTask);

            this.reEncryptJobInputFiles.ExecuteAsync(
                folder,
                postProcessingParameters.DecryptingTenantShortName,
                studyDownloadMetadata,
                cancellationToken).Returns(Task.CompletedTask);

            await this.target.ExecuteAsync(
                postProcessingParameters,
                folder,
                studyDownloadMetadata,
                cancellationToken);
            
            await this.patchJobInputFiles.Received(1).ExecuteAsync(folder, cancellationToken);

            await this.reEncryptJobInputFiles.Received(1).ExecuteAsync(
                folder,
                postProcessingParameters.DecryptingTenantShortName,
                studyDownloadMetadata,
                cancellationToken);

            await this.postProcessStudyDownload.DidNotReceive().ExecuteAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task ItShouldAlsoPostProcessIfPostProcessorPathSupplied()
        {
            var postProcessingParameters = new PostProcessingParameters("pp", "ppa", string.Empty);
            var folder = SingletonRandom.Instance.NextString();
            var studyDownloadMetadata = StudyDownloadMetadata.Random();
            var cancellationToken = new CancellationTokenSource().Token;
            
            this.patchJobInputFiles.ExecuteAsync(folder, cancellationToken).Returns(Task.CompletedTask);

            this.postProcessStudyDownload.ExecuteAsync(
                postProcessingParameters.PostProcessorPath,
                postProcessingParameters.PostProcessorArguments,
                folder).Returns(Task.CompletedTask);

            await this.target.ExecuteAsync(
                postProcessingParameters,
                folder,
                studyDownloadMetadata,
                cancellationToken);
            
            await this.patchJobInputFiles.Received(1).ExecuteAsync(folder, cancellationToken);

            await this.reEncryptJobInputFiles.DidNotReceive().ExecuteAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<StudyDownloadMetadata>(), cancellationToken);

            await this.postProcessStudyDownload.Received(1).ExecuteAsync(
                postProcessingParameters.PostProcessorPath,
                postProcessingParameters.PostProcessorArguments,
                folder);
        }
    }
}