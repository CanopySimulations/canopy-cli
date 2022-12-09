using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class PatchJobInputFilesTests
    {
        private readonly IPatchJobInputFile patchJobInputFile;
        private readonly IFileOperations fileOperations;

        private readonly PatchJobInputFiles target;

        public PatchJobInputFilesTests()
        {
            this.patchJobInputFile = Substitute.For<IPatchJobInputFile>();
            this.fileOperations = Substitute.For<IFileOperations>();

            this.target = new PatchJobInputFiles(
                this.patchJobInputFile,
                this.fileOperations);
        }

        [Fact]
        public async Task WhenStudyBaselineFileDoesNotExistItShouldDoNothing()
        {
            var folder = "/a";
            var cancellationToken = new CancellationTokenSource().Token;
            var studyBaselinePath = Path.Combine(folder, Constants.StudyBaselineFileName);

            this.fileOperations.Exists(studyBaselinePath).Returns(false);

            await this.target.ExecuteAsync(folder, cancellationToken);

            await this.fileOperations.DidNotReceive().ReadAllTextAsync(studyBaselinePath, cancellationToken);
        }

        [Fact]
        public async Task ItShouldPatchEachJobInputFile()
        {
            var folder = "/a";
            var cancellationToken = new CancellationTokenSource().Token;
            var studyBaselinePath = Path.Combine(folder, Constants.StudyBaselineFileName);

            this.fileOperations.Exists(studyBaselinePath).Returns(true);

            var studyBaselineContent = "sbc";
            this.fileOperations.ReadAllTextAsync(studyBaselinePath, cancellationToken).Returns(Task.FromResult(studyBaselineContent));

            var jobFilePath1 = "f-1";
            var jobFilePath2 = "f-2";
            this.fileOperations.GetFiles(folder, Constants.JobPatchFileName, SearchOption.AllDirectories).Returns(new []
            {
                jobFilePath1,
                jobFilePath2,
            });

            this.patchJobInputFile.ExecuteAsync(studyBaselineContent, Arg.Any<string>(), cancellationToken).Returns(Task.CompletedTask);

            await this.target.ExecuteAsync(folder, cancellationToken);

            await this.patchJobInputFile.Received(1).ExecuteAsync(studyBaselineContent, jobFilePath1, cancellationToken);
            await this.patchJobInputFile.Received(1).ExecuteAsync(studyBaselineContent, jobFilePath2, cancellationToken);
        }
    }
}