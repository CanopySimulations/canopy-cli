using System;

namespace Canopy.Cli.Executable.Services
{
    public interface IWriteFileMock : IWriteFile
    {
        int Count { get; }

        IDisposable Record();
        long GetSize(string path);
    }
}