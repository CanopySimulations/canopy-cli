using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IFileOperations
    {
        bool Exists(string path);
        string[] GetFiles(string path, string searchPattern, SearchOption searchOption);
        Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken);
        Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken);
    }
}