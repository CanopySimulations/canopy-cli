using Azure.Storage.DataMovement;
using Azure.Storage.DataMovement.Blobs;
using Canopy.Cli.Executable.Azure;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public class DownloadBlobDirectory(
        TransferManager transferManager,
        CheckpointDirectory checkpointDirectory,
        ILogger<DownloadBlobDirectory> logger) : IDownloadBlobDirectory
    {
        private const long MaximumTransferChunkSize = 20_971_520;

        public async Task<TransferOperation?> ExecuteAsync(
            BlobDirectory blobDirectory,
            string outputDirectoryPath,
            bool isRetry,
            TransferProgressHandlerOptions progressHandlerOptions,
            CancellationToken cancellationToken)
        {
            TransferOperation? operation = null;
            try
            {
                var transferOptions = new TransferOptions
                {
                    // First attempt: overwrite stale files from previous runs.
                    // Retry: skip files already downloaded in the first attempt.
                    CreationMode = isRetry
                        ? StorageResourceCreationMode.SkipIfExists
                        : StorageResourceCreationMode.OverwriteIfExists,
                    MaximumTransferChunkSize = MaximumTransferChunkSize,
                    ProgressHandlerOptions = progressHandlerOptions
                };

                var source = BlobsStorageResourceProvider.FromClient(
                    blobDirectory.Container,
                    new BlobStorageResourceContainerOptions { BlobPrefix = blobDirectory.Prefix });

                var destination = LocalFilesStorageResourceProvider.FromDirectory(outputDirectoryPath);

                operation = await transferManager.StartTransferAsync(source, destination, transferOptions, cancellationToken);
                await operation.WaitForCompletionAsync(cancellationToken);
                return operation;
            }
            catch (Exception t) when (ExceptionUtilities.IsFromCancellation(t))
            {
                return null;
            }
            finally
            {
                if (operation != null)
                {
                    TryDeleteCheckpointFile(operation.Id);
                }
            }
        }

        private void TryDeleteCheckpointFile(string operationId)
        {
            var checkpointDir = checkpointDirectory.Path;
            if (checkpointDir == null)
            {
                return;
            }

            // The SDK stores checkpoint files as {transferId}.json in the configured directory.
            var path = Path.Combine(checkpointDir, $"{operationId}.json");
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to clean up checkpoint file {Path}.", path);
            }
        }
    }
}