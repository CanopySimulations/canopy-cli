using System;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Shared
{
    public interface IFile
    {
        string FileName { get; }

        string FullPath { get; }

        string RelativePathToFile { get; }

        Task<byte[]> GetContentAsBytesAsync(CancellationToken cancellationToken = default) =>
            cancellationToken.IsCancellationRequested ? throw new OperationCanceledException() : GetContentAsBytesAsync();

        Task<byte[]> GetContentAsBytesAsync();

        Task<string> GetContentAsTextAsync(CancellationToken cancellationToken = default) =>
            cancellationToken.IsCancellationRequested ? throw new OperationCanceledException() : GetContentAsTextAsync();

        Task<string> GetContentAsTextAsync();

    }
    
}
