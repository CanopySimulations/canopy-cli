using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.DataMovement;

namespace Canopy.Cli.Executable.Services
{
    public class DownloadBlobDirectory : IDownloadBlobDirectory
    {
        public async Task<TransferStatus?> ExecuteAsync(
            CloudBlobDirectory blobDirectory,
            string outputDirectoryPath,
            DownloadDirectoryOptions options,
            DirectoryTransferContext context,
            CancellationToken cancellationToken)
        {
            try
            {
                return await TransferManager.DownloadDirectoryAsync(
                   blobDirectory,
                   outputDirectoryPath,
                   options,
                   context,
                   cancellationToken);
            }
            catch (Exception t) when (ExceptionUtilities.IsFromCancellation(t))
            {
                return null;
            }
        }
    }
}