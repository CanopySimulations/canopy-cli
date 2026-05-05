using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public class DownloadBlob : IDownloadBlob
    {
        private const long MaximumTransferSize = 20_971_520;

        public async Task ExecuteAsync(
            BlobContainerClient container,
            string blobName,
            string localPath,
            IProgress<long>? bytesProgress,
            Action? onCompleted,
            Action? onFailed,
            SemaphoreSlim semaphoreToRelease,
            CancellationToken cancellationToken)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

                await container.GetBlobClient(blobName).DownloadToAsync(
                    localPath,
                    new BlobDownloadToOptions
                    {
                        TransferOptions = new StorageTransferOptions
                        {
                            MaximumTransferSize = MaximumTransferSize,
                        },
                        ProgressHandler = bytesProgress,
                    },
                    cancellationToken);

                onCompleted?.Invoke();
            }
            catch (Exception t) when (ExceptionUtilities.IsFromCancellation(t))
            {
            }
            catch
            {
                onFailed?.Invoke();
            }
            finally
            {
                semaphoreToRelease.Release();
            }
        }
    }
}
