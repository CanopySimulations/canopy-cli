using System.IO;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class AddedDownloadTokensCacheTests
    {
        private readonly AddedDownloadTokensCache target = new AddedDownloadTokensCache();

        [Fact]
        public void ItShouldAddEachFileOnce()
        {
            var resultA1 = this.target.TryAdd("./tokens/a.token");
            var resultA2 = this.target.TryAdd("./tokens/a.token");
            var resultB1 = this.target.TryAdd("./tokens/b.token");
            var resultB2 = this.target.TryAdd("./tokens/blah/../b.token");
            var resultA3 = this.target.TryAdd("./tokens/blah/../a.token");

            Assert.True(resultA1);
            Assert.True(resultB1);

            Assert.False(resultA2);
            Assert.False(resultA3);
            Assert.False(resultB2);
        }
    }
}