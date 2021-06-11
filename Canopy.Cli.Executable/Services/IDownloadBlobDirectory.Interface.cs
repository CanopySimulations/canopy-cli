using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.DataMovement;

namespace Canopy.Cli.Executable.Services
{
    public interface IDownloadBlobDirectory
    {
        Task<TransferStatus?> ExecuteAsync(
            CloudBlobDirectory blobDirectory, 
            string outputDirectoryPath, 
            DownloadDirectoryOptions options, 
            DirectoryTransferContext context, 
            CancellationToken cancellationToken);
    }
}