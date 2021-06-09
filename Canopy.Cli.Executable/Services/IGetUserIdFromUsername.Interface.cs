namespace Canopy.Cli.Executable.Services
{
    using Canopy.Api.Client;
    using System.Threading.Tasks;

    public interface IGetUserIdFromUsername
    {
        Task<string?> ExecuteAsync(string tenantId, string username);
    }
}
