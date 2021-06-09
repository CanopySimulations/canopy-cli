using NSubstitute;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class GetAvailableOutputFolderTests
    {
        private static readonly string OutputFolderPath = "C:\\foo\\bar";

        private readonly IDirectoryExists directoryExists;

        private readonly GetAvailableOutputFolder target;

        public GetAvailableOutputFolderTests()
        {
            this.directoryExists = Substitute.For<IDirectoryExists>();

            this.target = new GetAvailableOutputFolder(this.directoryExists);
        }

        [Fact]
        public void WhenFolderDoesNotExistItShouldReturnOriginalFolderName()
        {
            this.directoryExists.Execute(OutputFolderPath).Returns(false);

            var result = this.target.Execute(OutputFolderPath);

            Assert.Equal(OutputFolderPath, result);
        }

        [Fact]
        public void WhenFolderExistsItShouldReturnUniqueFolderName()
        {
            this.directoryExists.Execute(OutputFolderPath).Returns(true);
            this.directoryExists.Execute(OutputFolderPath + " (1)").Returns(true);
            this.directoryExists.Execute(OutputFolderPath + " (2)").Returns(false);

            var result = this.target.Execute(OutputFolderPath);

            Assert.Equal(OutputFolderPath + " (2)", result);
        }
    }
}