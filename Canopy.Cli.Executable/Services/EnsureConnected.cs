using Canopy.Api.Client;

namespace Canopy.Cli.Executable.Services
{
    public class EnsureConnected : IEnsureConnected
    {
        private readonly IConnectionManager connectionManager;

        public EnsureConnected(IConnectionManager connectionManager)
        {
            this.connectionManager = connectionManager;
        }

        public void Execute()
        {
            if (!this.connectionManager.LoadConnectionInformation())
            {
                throw new RecoverableException("Not connected. Run `canopy connect --help` for more information.");
            }
        }
    }
}