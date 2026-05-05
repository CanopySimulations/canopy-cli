using System.Threading;
using System.Threading.Tasks;
using Canopy.Cli.Executable.Azure;

namespace Canopy.Cli.Executable.Services
{
    public interface IDownloadBlobDirectory
    {
        Task ExecuteAsync(
            BlobDirectory blobDirectory,
            string outputDirectoryPath,
            BlobDirectoryDownloadOptions options,
            CancellationToken cancellationToken);
    }
}
