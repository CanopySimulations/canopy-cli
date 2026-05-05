using Azure.Storage.Blobs.Models;
using Canopy.Cli.Executable.Azure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public class DownloadBlobDirectory(IDownloadBlob downloadBlob) : IDownloadBlobDirectory
    {
        public async Task ExecuteAsync(
            BlobDirectory blobDirectory,
            string outputDirectoryPath,
            BlobDirectoryDownloadOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                long[] totalBytesTransferred = [0];
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

                    // Wait for the semaphore before starting the download, and release it when the download completes
                    // (successfully or with failure, inside downloadBlob.ExecuteAsync()).
                    // This is to control how many download tasks run concurrently
                    await semaphore.WaitAsync(cancellationToken);
                    tasks.Add(downloadBlob.ExecuteAsync(
                        blobDirectory.Container,
                        blobName,
                        localPath,
                        CreateBytesProgress(options.BytesProgress, totalBytesTransferred),
                        options.OnFileCompleted,
                        options.OnFileFailed,
                        semaphore,
                        cancellationToken));
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception t) when (ExceptionUtilities.IsFromCancellation(t))
            {
            }
        }

        private static IProgress<long>? CreateBytesProgress(Action<long>? bytesProgress, long[] totalBytesTransferred)
        {
            if (bytesProgress == null)
            {
                return null;
            }

            long[] filePreviousBytes = [0];
            return new SyncProgress<long>(bytes =>
            {
                var prev = Interlocked.Exchange(ref filePreviousBytes[0], bytes);
                var delta = bytes - prev;
                if (delta > 0)
                    bytesProgress(Interlocked.Add(ref totalBytesTransferred[0], delta));
            });
        }

        private sealed class SyncProgress<T>(Action<T> callback) : IProgress<T>
        {
            public void Report(T value) => callback(value);
        }
    }
}

