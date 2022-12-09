using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class PatchJobInputFileTests
    {
        private readonly IFileOperations fileOperations;

        private readonly PatchJobInputFile target;

        public PatchJobInputFileTests()
        {
            this.fileOperations = Substitute.For<IFileOperations>();

            this.target = new PatchJobInputFile(
                this.fileOperations);
        }

        [Fact]
        public async Task WhenNoPatchFilePathItShouldDoNothing()
        {
            var studyBaselineText = "sbt";
            var patchFilePath = string.Empty;
            var cancellationToken = new CancellationTokenSource().Token;

            await this.target.ExecuteAsync(studyBaselineText, patchFilePath, cancellationToken);

            this.fileOperations.DidNotReceive().Exists(Arg.Any<string>());
        }

        [Fact]
        public async Task WhenJobFileAlreadyExistsItShouldDoNothing()
        {
            var studyBaselineText = "sbt";
            var patchFilePath = "/a/job.patch";
            var cancellationToken = new CancellationTokenSource().Token;

            var jobFilePath = Path.Combine(Path.GetDirectoryName(patchFilePath)!, Constants.JobFileName);

            this.fileOperations.Exists(jobFilePath).Returns(true);

            await this.target.ExecuteAsync(studyBaselineText, patchFilePath, cancellationToken);

            this.fileOperations.Received(1).Exists(jobFilePath);
            await this.fileOperations.DidNotReceive().ReadAllTextAsync(patchFilePath, cancellationToken);
        }

        [Fact]
        public async Task ItShouldSavePatchedFile()
        {
            static string SanitizeText(string input) => input.Replace("\r", string.Empty);

            var studyBaselineText = SanitizeText(@"The quick brown fox
jumps over
the lazy dog.");

            var expectedOutput = SanitizeText(@"The quick brown fox
leaps over
the lazy dog.");

            var patchFileText = SanitizeText(@"@@ -18,11 +18,11 @@
 ox%0d%0a
-jum
+lea
 ps o
");
            var patchFilePath = "/a/job.patch";
            var cancellationToken = new CancellationTokenSource().Token;
            var jobFilePath = Path.Combine(Path.GetDirectoryName(patchFilePath)!, Constants.JobFileName);

            this.fileOperations.Exists(jobFilePath).Returns(false);
            this.fileOperations.ReadAllTextAsync(patchFilePath, cancellationToken).Returns(Task.FromResult(patchFileText));

            await this.target.ExecuteAsync(studyBaselineText, patchFilePath, cancellationToken);

            this.fileOperations.Received(1).Exists(jobFilePath);
            await this.fileOperations.Received(1).WriteAllTextAsync(jobFilePath, expectedOutput, cancellationToken);
        }
    }
}