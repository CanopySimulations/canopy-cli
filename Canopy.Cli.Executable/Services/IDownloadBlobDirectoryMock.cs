using System;

namespace Canopy.Cli.Executable.Services
{
    public interface IDownloadBlobDirectoryMock : IDownloadBlobDirectory
    {
        int Count { get; }
        IDisposable Record();
    }
}