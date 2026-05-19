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
            var prefix = blobDirectory.Prefix.TrimEnd('/') + '/';
            var semaphore = options.ConcurrencySemaphore;
            var tasks = new List<Task>();

            try
            {
                await foreach (var blobItem in blobDirectory.Container.GetBlobsAsync(
                    BlobTraits.None,
                    BlobStates.None,
                    prefix,
                    cancellationToken))
                {
                    var blobName = blobItem.Name;
                    if (blobName.EndsWith('/'))
                    {
                        continue;
                    }

                    var localPath = Path.Combine(
                        outputDirectoryPath,
                        blobName[prefix.Length..].Replace('/', Path.DirectorySeparatorChar));

                    if (options.SkipExistingFiles && File.Exists(localPath))
                    {
                        options.OnFileSkipped?.Invoke();
                        continue;
                    }

                    await semaphore.WaitAsync(cancellationToken);
                    tasks.Add(ReleaseAfter(
                        downloadBlob.ExecuteAsync(
                            blobDirectory.Container,
                            blobName,
                            localPath,
                            CreateBytesProgress(options.BytesProgress),
                            options.OnFileCompleted,
                            options.OnFileFailed,
                            cancellationToken),
                        semaphore));
                }
            }
            finally
            {
                await Task.WhenAll(tasks);
            }
        }

        private static async Task ReleaseAfter(Task task, SemaphoreSlim semaphore)
        {
            try { await task; }
            finally { semaphore.Release(); }
        }

        private static IProgress<long>? CreateBytesProgress(Action<long>? bytesProgress)
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
                    bytesProgress(delta);
            });
        }

        private sealed class SyncProgress<T>(Action<T> callback) : IProgress<T>
        {
            public void Report(T value) => callback(value);
        }
    }
}

