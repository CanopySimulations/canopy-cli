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
            
            Assert.Equal(@"C:\Temp\Invalid Folder -Name- #simVersion=-1.4862-", this.target.Execute(@"C:\Temp\Invalid Folder <Name> #simVersion=""1.4862"""));
        }
    }
}