using NSubstitute;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class GetAvailableOutputFolderTests
    {
        private static readonly string OutputFolderPath = "C:\\foo\\bar<>";
        private static readonly string SanitizedOutputFolderPath = "C:\\foo\\bar--";

        private readonly IGetPathWithSanitizedFolderName getPathWithSanitizedFolderName;
        private readonly IDirectoryExists directoryExists;

        private readonly GetAvailableOutputFolder target;

        public GetAvailableOutputFolderTests()
        {
            this.getPathWithSanitizedFolderName = Substitute.For<IGetPathWithSanitizedFolderName>();
            this.directoryExists = Substitute.For<IDirectoryExists>();

            this.getPathWithSanitizedFolderName.Execute(OutputFolderPath).Returns(SanitizedOutputFolderPath);

            this.target = new GetAvailableOutputFolder(
                this.getPathWithSanitizedFolderName,
                this.directoryExists);
        }

        [Fact]
        public void WhenFolderDoesNotExistItShouldReturnOriginalFolderName()
        {
            this.directoryExists.Execute(SanitizedOutputFolderPath).Returns(false);

            var result = this.target.Execute(OutputFolderPath);

            Assert.Equal(SanitizedOutputFolderPath, result);
        }

        [Fact]
        public void WhenFolderExistsItShouldReturnUniqueFolderName()
        {
            this.directoryExists.Execute(SanitizedOutputFolderPath).Returns(true);
            this.directoryExists.Execute(SanitizedOutputFolderPath + " (1)").Returns(true);
            this.directoryExists.Execute(SanitizedOutputFolderPath + " (2)").Returns(false);

            var result = this.target.Execute(OutputFolderPath);

            Assert.Equal(SanitizedOutputFolderPath + " (2)", result);
        }
    }
}