using Canopy.Cli.Executable.Azure;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public interface IGetStudyBlobDirectory
    {
        BlobDirectory Execute(BlobAccessInformation accessInformation);
        BlobDirectory Execute(string url, string accessSignature);
    }
}