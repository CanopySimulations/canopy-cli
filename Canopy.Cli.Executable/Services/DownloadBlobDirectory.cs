using Azure.Storage.DataMovement;
using Azure.Storage.DataMovement.Blobs;
using Canopy.Cli.Executable.Azure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public class DownloadBlobDirectory(
        TransferManagerOptions transferManagerOptions) : IDownloadBlobDirectory
    {
        public async Task ExecuteAsync(
            BlobDirectory blobDirectory,
            string outputDirectoryPath,
            TransferOptions transferOptions,
            CancellationToken cancellationToken)
        {
            try
            {
                await using var transferManager = new TransferManager(transferManagerOptions);

                var source = BlobsStorageResourceProvider.FromClient(
                    blobDirectory.Container,
                    new BlobStorageResourceContainerOptions { BlobPrefix = blobDirectory.Prefix });

                var destination = LocalFilesStorageResourceProvider.FromDirectory(outputDirectoryPath);

                var operation = await transferManager.StartTransferAsync(source, destination, transferOptions, cancellationToken);
                await operation.WaitForCompletionAsync(cancellationToken);
            }
            catch (Exception t) when (ExceptionUtilities.IsFromCancellation(t))
            {
            }
        }

    }
}