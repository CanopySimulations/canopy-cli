using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Canopy.Cli.Shared;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class ReEncryptJobInputFilesTests
    {
        private readonly IFileOperations fileOperations;
        private readonly IContainsEncryptedToken containsEncryptedToken;
        private readonly IReEncryptFile reEncryptFile;
        private readonly ILogger<ReEncryptJobInputFiles> logger;

        private readonly ReEncryptJobInputFiles target;

        public ReEncryptJobInputFilesTests()
        {
            this.fileOperations = Substitute.For<IFileOperations>();
            this.containsEncryptedToken = Substitute.For<IContainsEncryptedToken>();
            this.reEncryptFile = Substitute.For<IReEncryptFile>();
            this.logger = Substitute.For<ILogger<ReEncryptJobInputFiles>>();

            this.target = new ReEncryptJobInputFiles(
                this.fileOperations,
                this.containsEncryptedToken,
                this.reEncryptFile,
                this.logger);
        }

        [Fact]
        public async Task WhenNoDecryptingTenantShortNameItShouldDoNothing()
        {
            var folder = SingletonRandom.Instance.NextString();
            var decryptingTenantShortName = string.Empty;
            var studyDownloadMetadata = StudyDownloadMetadata.Random();
            var cancellationToken = new CancellationTokenSource().Token;

            await this.target.ExecuteAsync(folder, decryptingTenantShortName, studyDownloadMetadata, cancellationToken);

            this.fileOperations.DidNotReceiveWithAnyArgs().GetFiles(string.Empty, string.Empty, default);
        }

        [Fact]
        public async Task ItShouldReplaceJobFilesWithReEncryptedVersions()
        {
            var folder = @"/a";
            var decryptingTenantShortName = SingletonRandom.Instance.NextString();
            var studyDownloadMetadata = StudyDownloadMetadata.Random();
            var cancellationToken = new CancellationTokenSource().Token;

            var jobFilePath1 = @"/a/1/job.json";
            var jobFilePath2 = @"/a/2/job.json";
            var jobFilePath3 = @"/a/3/job.json";

            var studyBaselinePath = Path.Combine(folder, Constants.StudyBaselineFileName);

            this.fileOperations.GetFiles(folder, Constants.JobFileName, System.IO.SearchOption.AllDirectories)
                .Returns(new []
                {
                    jobFilePath1,
                    jobFilePath2,
                    jobFilePath3,
                });

            this.fileOperations.Exists(jobFilePath1).Returns(true);
            this.fileOperations.Exists(jobFilePath2).Returns(false);
            this.fileOperations.Exists(jobFilePath3).Returns(true);
            this.fileOperations.Exists(studyBaselinePath).Returns(true);

            var jobContent1 = "j-1";
            var jobContent3 = "j-3";
            var studyBaselineContent = "sb-1";

            this.fileOperations.ReadAllTextAsync(jobFilePath1, cancellationToken).Returns(Task.FromResult(jobContent1));
            this.fileOperations.ReadAllTextAsync(jobFilePath3, cancellationToken).Returns(Task.FromResult(jobContent3));
            this.fileOperations.ReadAllTextAsync(studyBaselinePath, cancellationToken).Returns(Task.FromResult(studyBaselineContent));

            this.containsEncryptedToken.Execute(jobContent1).Returns(true);
            this.containsEncryptedToken.Execute(jobContent3).Returns(false);
            this.containsEncryptedToken.Execute(studyBaselineContent).Returns(true);

            var newJobContent1 = "new-j-1";
            var newStudyBaselineContent = "new-sb-1";

            this.reEncryptFile.ExecuteAsync(jobContent1, decryptingTenantShortName, studyDownloadMetadata.SimVersion, cancellationToken).Returns(newJobContent1);
            this.reEncryptFile.ExecuteAsync(studyBaselineContent, decryptingTenantShortName, studyDownloadMetadata.SimVersion, cancellationToken).Returns(newStudyBaselineContent);

            this.fileOperations.WriteAllTextAsync(Arg.Any<string>(), Arg.Any<string>(), cancellationToken).Returns(Task.CompletedTask);

            await this.target.ExecuteAsync(folder, decryptingTenantShortName, studyDownloadMetadata, cancellationToken);
            
            await this.fileOperations.DidNotReceive().ReadAllTextAsync(jobFilePath2, cancellationToken);

            await this.fileOperations.Received(1).WriteAllTextAsync(jobFilePath1, newJobContent1, cancellationToken);
            await this.fileOperations.Received(1).WriteAllTextAsync(jobFilePath1 + Constants.OrignalFileSuffix, jobContent1, cancellationToken);
            
            await this.fileOperations.Received(1).WriteAllTextAsync(studyBaselinePath, newStudyBaselineContent, cancellationToken);
            await this.fileOperations.Received(1).WriteAllTextAsync(studyBaselinePath + Constants.OrignalFileSuffix, studyBaselineContent, cancellationToken);
        }
    }
}