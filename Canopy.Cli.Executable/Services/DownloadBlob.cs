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
        private const long TransferSize = 20_971_520;
        private static readonly int ChunkConcurrency = Environment.ProcessorCount;

        public async Task ExecuteAsync(
            BlobContainerClient container,
            string blobName,
            string localPath,
            IProgress<long>? bytesProgress,
            Action? onCompleted,
            Action? onFailed,
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
                            InitialTransferSize = TransferSize,
                            MaximumTransferSize = TransferSize,
                            MaximumConcurrency = ChunkConcurrency,
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
        }
    }
}
