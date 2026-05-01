using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.DataMovement;
using Canopy.Cli.Executable.Azure;

namespace Canopy.Cli.Executable.Services
{
    public interface IDownloadBlobDirectory
    {
        Task ExecuteAsync(
            BlobDirectory blobDirectory,
            string outputDirectoryPath,
            TransferOptions transferOptions,
            CancellationToken cancellationToken);
    }
}