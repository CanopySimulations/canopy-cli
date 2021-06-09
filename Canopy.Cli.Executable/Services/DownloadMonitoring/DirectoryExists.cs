using System.IO;
namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class DirectoryExists : IDirectoryExists
    {
        public bool Execute(string path)
        {
            return Directory.Exists(path);
        }
    }
}