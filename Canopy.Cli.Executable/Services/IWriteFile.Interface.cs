using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public interface IWriteFile
    {
        Task ExecuteAsync(string path, string content, CancellationToken cancellationToken = default);
    }
}