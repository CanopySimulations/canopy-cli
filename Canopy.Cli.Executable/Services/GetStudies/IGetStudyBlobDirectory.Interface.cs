using Microsoft.Azure.Storage.Blob;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public interface IGetStudyBlobDirectory
    {
        CloudBlobDirectory Execute(BlobAccessInformation accessInformation);
        CloudBlobDirectory Execute(string url, string accessSignature);
    }
}