using System;
using System.Threading.Tasks;
using Canopy.Api.Client;

namespace Canopy.Cli.Executable.Commands
{
	public class UnauthenticateCommand : CanopyCommandBase
	{
		public UnauthenticateCommand()
		{
            this.RequiresConnection = false;
            this.RequiresAuthentication = false;

            this.Name = "unauthenticate";
			this.Description = "Unauthenticates from the API and removes local sign-in information.";
		}

		protected override Task<int> ExecuteAsync()
		{
            AuthenticationManager.Instance.ClearAuthenticatedUser();
			return Task.FromResult(0);
		}
	}
}
