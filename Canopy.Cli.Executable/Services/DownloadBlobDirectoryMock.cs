using Canopy.Cli.Executable.Azure;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public class DownloadBlobDirectoryMock : IDownloadBlobDirectory, IDownloadBlobDirectoryMock
    {
        public record Request(BlobDirectory BlobDirectory, string OutputDirectoryPath);

        private bool isRecording = false;

        private readonly ConcurrentBag<Request> requests = new();

        public Task ExecuteAsync(
            BlobDirectory blobDirectory,
            string outputDirectoryPath,
            BlobDirectoryDownloadOptions options,
            CancellationToken cancellationToken)
        {
            if (!this.isRecording)
            {
                throw new InvalidOperationException($"{nameof(DownloadBlobDirectoryMock)} is not recording.");
            }

            this.requests.Add(new Request(blobDirectory, outputDirectoryPath));

            return Task.CompletedTask;
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
