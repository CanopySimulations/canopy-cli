using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public interface ISimVersionCache
    {
        Task<string> Get(CancellationToken cancellationToken = default);
        Task<string> GetOrSet(string? requestedSimVersion, CancellationToken cancellationToken = default);
        void Set(string simVersion);
    }
}