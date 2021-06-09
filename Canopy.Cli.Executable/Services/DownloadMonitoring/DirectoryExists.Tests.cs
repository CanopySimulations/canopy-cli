using Canopy.Cli.Shared;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class DirectoryExistsTests
    {
        private readonly DirectoryExists target = new DirectoryExists();

        [Fact]
        public void ItShouldReturnTrueIfDirectoryExists()
        {
            Assert.True(this.target.Execute("."));
        }

        [Fact]
        public void ItShouldReturnFalseIfDirectoryDoesNotExist()
        {
            Assert.False(this.target.Execute("./" + SingletonRandom.Instance.NextString()));
        }
    }
}