using System.Linq;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class MoveCompletedDownloadTokenTests
    {
        private readonly MoveCompletedDownloadToken target = new();

        [Fact]
        public async Task ItShouldMoveTokensAndRename()
        {
            using var tempFolder = TestUtilities.GetTempFolder();

            var tokenPath = Path.Combine(tempFolder.Path, "input" + DownloaderConstants.DownloadTokenExtensionWithPeriod);

            await File.WriteAllTextAsync(
                tokenPath, 
                "a");

            var outputDirectoryPath =  Path.Combine(tempFolder.Path, "output");
            
            Directory.CreateDirectory(outputDirectoryPath);

            this.target.Execute(tokenPath, outputDirectoryPath);

            Assert.Single(Directory.GetFiles(outputDirectoryPath));
            Assert.True(File.Exists(Path.Combine(outputDirectoryPath, DownloaderConstants.CompletedDownloadTokenFileName)));
            Assert.False(File.Exists(tokenPath));
        }        
    }
}