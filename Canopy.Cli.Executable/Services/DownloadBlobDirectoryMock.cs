using System.Threading;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.DataMovement;

namespace Canopy.Cli.Executable.Services
{
    public class DownloadBlobDirectoryMock : IDownloadBlobDirectory, IDownloadBlobDirectoryMock
    {
        public record Request(CloudBlobDirectory BlobDirectory, string outputDirectoryPath);

        private bool isRecording = false;

        private readonly ConcurrentBag<Request> requests = new();

        public Task<TransferStatus?> ExecuteAsync(
            CloudBlobDirectory blobDirectory,
            string outputDirectoryPath,
            DownloadDirectoryOptions options,
            DirectoryTransferContext context,
            CancellationToken cancellationToken)
        {
            if (!this.isRecording)
            {
                throw new InvalidOperationException($"{nameof(DownloadBlobDirectoryMock)} is not recording.");
            }

            this.requests.Add(new Request(blobDirectory, outputDirectoryPath));

            return Task.FromResult<TransferStatus?>(null);
        }

        public IDisposable Record()
        {
            this.requests.Clear();
            this.isRecording = true;
            return new Token(() =>
            {
                this.isRecording = false;
                this.requests.Clear();
            });
        }

        public int Count => this.requests.Count;

        public class Token : IDisposable
        {
            private readonly Action onDispose;

            public Token(Action onDispose)
            {
                this.onDispose = onDispose;
            }

            public void Dispose()
            {
                this.onDispose();
            }
        }
    }
}