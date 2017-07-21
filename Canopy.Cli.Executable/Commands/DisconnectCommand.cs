using System;
using System.Threading.Tasks;
using Canopy.Api.Client;

namespace Canopy.Cli.Executable.Commands
{
    public class DisconnectCommand : CanopyCommandBase
    {
        public DisconnectCommand()
        {
            this.RequiresConnection = false;
            this.RequiresAuthentication = false;

            this.Name = "disconnect";
			this.Description = "Disconnects from the API endpoint, and removes any authentication information.";
		}

		protected override Task<int> ExecuteAsync()
		{
			AuthenticationManager.Instance.ClearAuthenticatedUser();
			ConnectionManager.Instance.ClearConnectionInformation();
            return Task.FromResult(0);
		}
    }
}
