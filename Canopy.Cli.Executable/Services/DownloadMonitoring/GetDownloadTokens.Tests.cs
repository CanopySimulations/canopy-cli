using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class GetDownloadTokensTests
    {
        private readonly GetDownloadTokens target = new();

        [Fact]
        public async Task ItShouldReturnAllTokens()
        {
            using var tempFolder = TestUtilities.GetTempFolder();

            var filePath1 = Path.Combine(tempFolder.Path, "a" + DownloaderConstants.DownloadTokenExtensionWithPeriod);
            await File.WriteAllTextAsync(filePath1, "a");

            var filePath2 = Path.Combine(tempFolder.Path, "b" + DownloaderConstants.DownloadTokenExtensionWithPeriod + ".other");
            await File.WriteAllTextAsync(filePath2, "b");

            var filePath3 = Path.Combine(tempFolder.Path, "c" + DownloaderConstants.DownloadTokenExtensionWithPeriod);
            await File.WriteAllTextAsync(filePath3, "c");

            var filePath4 = Path.Combine(tempFolder.Path, "d.blah");
            await File.WriteAllTextAsync(filePath4, "d");

            var result = this.target.Execute(tempFolder.Path);

            Assert.Equal(
                new[]
                {
                    filePath1,
                    filePath3,
                },
                result.OrderBy(v => v).ToList());
        }

    }
}