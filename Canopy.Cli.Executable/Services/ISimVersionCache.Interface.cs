using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public interface ISimVersionCache
    {
        Task<string> Get();
        Task<string> GetOrSet(string? requestedSimVersion);
        void Set(string simVersion);
    }
}