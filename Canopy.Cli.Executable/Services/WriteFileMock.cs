using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public class WriteFileMock : IWriteFile, IWriteFileMock
    {
        private bool isRecording = false;
        private readonly ConcurrentDictionary<string, long> writtenSizes = new();

        public Task ExecuteAsync(string path, string content)
        {
            if (!this.isRecording)
            {
                throw new InvalidOperationException($"{nameof(WriteFileMock)} is not recording.");
            }

            this.writtenSizes.AddOrUpdate(path, path => content.Length, (path, existing) => content.Length);
            return Task.CompletedTask;
        }

        public IDisposable Record()
        {
            this.writtenSizes.Clear();
            this.isRecording = true;
            return new Token(() =>
            {
                this.isRecording = false;
                this.writtenSizes.Clear();
            });
        }

        public int Count => this.writtenSizes.Count;

        public long GetSize(string path)
        {
            if (writtenSizes.TryGetValue(path, out var size))
            {
                return size;
            }

            throw new ArgumentException($"Path {path} was not written.");
        }

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