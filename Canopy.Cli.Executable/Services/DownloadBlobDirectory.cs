using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Canopy.Cli.Executable.Azure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public class DownloadBlobDirectory : IDownloadBlobDirectory
    {
        private const long MaximumTransferSize = 20_971_520;

        public async Task ExecuteAsync(
            BlobDirectory blobDirectory,
            string outputDirectoryPath,
            BlobDirectoryDownloadOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                var totalBytesTransferred = new long[1];
                var prefix = blobDirectory.Prefix.TrimEnd('/') + '/';
                var semaphore = options.ConcurrencySemaphore;
                var tasks = new List<Task>();

                await foreach (var blobItem in blobDirectory.Container.GetBlobsAsync(
                    BlobTraits.None,
                    BlobStates.None,
                    prefix,
                    cancellationToken))
                {
                    var blobName = blobItem.Name;
                    var localPath = Path.Combine(
                        outputDirectoryPath,
                        blobName[prefix.Length..].Replace('/', Path.DirectorySeparatorChar));

                    if (options.SkipExistingFiles && File.Exists(localPath))
                    {
                        options.OnFileSkipped?.Invoke();
                        continue;
                    }

                    await semaphore.WaitAsync(cancellationToken);
                    tasks.Add(DownloadBlobAsync(
                        blobDirectory.Container, blobName, localPath,
                        options, totalBytesTransferred, semaphore, cancellationToken));
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception t) when (ExceptionUtilities.IsFromCancellation(t))
            {
            }
        }

        private static async Task DownloadBlobAsync(
            BlobContainerClient container,
            string blobName,
            string localPath,
            BlobDirectoryDownloadOptions options,
            long[] totalBytesTransferred,
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

                long filePreviousBytes = 0;
                await container.GetBlobClient(blobName).DownloadToAsync(
                    localPath,
                    new BlobDownloadToOptions
                    {
                        TransferOptions = new StorageTransferOptions
                        {
                            MaximumTransferSize = MaximumTransferSize,
                        },
                        ProgressHandler = options.BytesProgress == null ? null : new SyncProgress(bytes =>
                        {
                            var prev = Interlocked.Exchange(ref filePreviousBytes, bytes);
                            var delta = bytes - prev;
                            if (delta > 0)
                                options.BytesProgress(Interlocked.Add(ref totalBytesTransferred[0], delta));
                        }),
                    },
                    cancellationToken);

                options.OnFileCompleted?.Invoke();
            }
            catch (Exception t) when (ExceptionUtilities.IsFromCancellation(t))
            {
            }
            catch
            {
                options.OnFileFailed?.Invoke();
            }
            finally
            {
                semaphore.Release();
            }
        }

        private sealed class SyncProgress(Action<long> callback) : IProgress<long>
        {
            public void Report(long value) => callback(value);
        }
    }
}
