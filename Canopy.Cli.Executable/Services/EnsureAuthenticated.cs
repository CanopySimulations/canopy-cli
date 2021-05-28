using System.Threading.Tasks;
using Canopy.Api.Client;

namespace Canopy.Cli.Executable.Services
{
    public class EnsureAuthenticated : IEnsureAuthenticated
    {
        private readonly IEnsureConnected ensureConnected;
        private readonly IAuthenticationManager authenticationManager;

        public EnsureAuthenticated(
            IEnsureConnected ensureConnected,
            IAuthenticationManager authenticationManager)
        {
            this.authenticationManager = authenticationManager;
            this.ensureConnected = ensureConnected;
        }

        public async Task<AuthenticatedUser> ExecuteAsync()
        {
            this.ensureConnected.Execute();

            if (!this.authenticationManager.LoadAuthenticatedUser())
            {
                throw new RecoverableException("Not authenticated. Run `canopy authenticate --help` for more information.");
            }

            return await this.authenticationManager.GetAuthenticatedUser();
        }
    }
}