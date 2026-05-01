using System;
using System.Threading;

namespace Canopy.Cli.Executable.Services
{
    public class BlobDirectoryDownloadOptions
    {
        public bool SkipExistingFiles { get; init; }
        public required SemaphoreSlim ConcurrencySemaphore { get; init; }
        public Action<long>? BytesProgress { get; init; }
        public Action? OnFileCompleted { get; init; }
        public Action? OnFileFailed { get; init; }
        public Action? OnFileSkipped { get; init; }
    }
}
