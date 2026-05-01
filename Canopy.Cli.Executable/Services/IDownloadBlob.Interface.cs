using Azure.Storage.Blobs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public interface IDownloadBlob
    {
        Task ExecuteAsync(
            BlobContainerClient container,
            string blobName,
            string localPath,
            IProgress<long>? bytesProgress,
            Action? onCompleted,
            Action? onFailed,
            SemaphoreSlim semaphoreToRelease,
            CancellationToken cancellationToken);
    }
}
