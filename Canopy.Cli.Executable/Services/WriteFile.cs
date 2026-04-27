using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public class WriteFile : IWriteFile
    {
        public Task ExecuteAsync(string path, string content, CancellationToken cancellationToken = default)
        {
            return File.WriteAllTextAsync(
                path,
                content,
                cancellationToken);
        }
    }
}