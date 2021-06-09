using System.Threading.Tasks;
using Canopy.Api.Client;

namespace Canopy.Cli.Executable.Services
{
    public interface IEnsureAuthenticated
    {
        Task<AuthenticatedUser> ExecuteAsync();
    }
}