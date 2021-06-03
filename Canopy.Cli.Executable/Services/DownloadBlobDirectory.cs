using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.DataMovement;

namespace Canopy.Cli.Executable.Services
{
    public class DownloadBlobDirectory : IDownloadBlobDirectory
    {
        public Task ExecuteAsync(
            CloudBlobDirectory blobDirectory,
            string outputDirectoryPath,
            DownloadDirectoryOptions options,
            DirectoryTransferContext context)
        {
            return TransferManager.DownloadDirectoryAsync(
                blobDirectory,
                outputDirectoryPath,
                options,
                context);
        }
    }
}