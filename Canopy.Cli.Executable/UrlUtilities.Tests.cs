using Xunit;

namespace Canopy.Cli.Executable
{
    public class UrlUtilitiesTests
    {
        public class AppendFolderToUrlTests
        {
            [Fact]
            public void ItShouldSupportUrlWithoutTrailingSeparator()
            {
                Assert.Equal(
                    "http://foo.com/a/b/",
                    UrlUtilities.AppendFolderToUrl("http://foo.com/a", "b"));
            }

            [Fact]
            public void ItShouldSupportUrlWithTrailingSeparator()
            {
                Assert.Equal(
                    "http://foo.com/a/b/",
                    UrlUtilities.AppendFolderToUrl("http://foo.com/a/", "b"));
            }
        }
        
    }
}