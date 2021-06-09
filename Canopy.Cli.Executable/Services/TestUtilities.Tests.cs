using System;
using System.IO;

namespace Canopy.Cli.Executable.Services
{
    public static class TestUtilities
    {
        // https://stackoverflow.com/a/20445952/37725
        public static TempFolderToken GetTempFolder()
        {
            string tempFolder = Path.GetTempFileName();
            File.Delete(tempFolder);
            Directory.CreateDirectory(tempFolder);
            return new TempFolderToken(tempFolder);
        }

        public class TempFolderToken : IDisposable
        {
            public TempFolderToken(string path)
            {
                this.Path = path;
            }

            public string Path { get; init; }

            public void Dispose()
            {
                Directory.Delete(this.Path, true);
            }
        }
    }
}