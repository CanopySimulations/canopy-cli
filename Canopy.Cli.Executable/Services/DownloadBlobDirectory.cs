using Azure.Storage.DataMovement;
using Azure.Storage.DataMovement.Blobs;
using Canopy.Cli.Executable.Azure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public class DownloadBlobDirectory(
        TransferManager transferManager) : IDownloadBlobDirectory
    {
        public async Task<TransferOperation?> ExecuteAsync(
            BlobDirectory blobDirectory,
            string outputDirectoryPath,
            TransferOptions transferOptions,
            CancellationToken cancellationToken)
        {
            try
            {
                var source = BlobsStorageResourceProvider.FromClient(
                    blobDirectory.Container,
                    new BlobStorageResourceContainerOptions { BlobPrefix = blobDirectory.Prefix });

                var destination = LocalFilesStorageResourceProvider.FromDirectory(outputDirectoryPath);

                var operation = await transferManager.StartTransferAsync(source, destination, transferOptions, cancellationToken);
                await operation.WaitForCompletionAsync(cancellationToken);
                return operation;
            }
            catch (Exception t) when (ExceptionUtilities.IsFromCancellation(t))
            {
                return null;
            }
        }

    }
}