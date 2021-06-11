using System.Runtime.InteropServices;
using Xunit;

namespace Canopy.Cli.Executable.Services
{
    public class GetPathWithSanitizedFolderNameTests
    {
        private readonly GetPathWithSanitizedFolderName target = new();

        [Fact]
        public void ItShouldSanitizeTheFinalFolderIfRequired()
        {
            Assert.Equal(@"C:\Temp\Valid Folder Name (1)", this.target.Execute(@"C:\Temp\Valid Folder Name (1)"));

            var pathWithInvalidFolderCharacters = "C:\\Temp\\Invalid Folder \0 <Name> #simVersion=\"1.4862\"";

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Equal(
                    @"C:\Temp\Invalid Folder - -Name- #simVersion=-1.4862-",
                    this.target.Execute(pathWithInvalidFolderCharacters));
            }
            else
            {
                Assert.Equal(
                    @"C:\Temp\Invalid Folder - <Name> #simVersion=""1.4862""",
                    this.target.Execute(pathWithInvalidFolderCharacters));
            }
        }
    }
}