using System;
using System.Threading.Tasks;
using Canopy.Api.Client;

namespace Canopy.Cli.Executable.Commands
{
	public class UnauthenticateCommand : CanopyCommandBase
	{
		public UnauthenticateCommand()
		{
			this.Name = "unauthenticate";
			this.Description = "Unauthenticates from the API and removes local sign-in information.";
		}

		protected override Task ExecuteAsync()
		{
            AuthenticationManager.Instance.ClearAuthenticatedUser();
			return Task.CompletedTask;
		}
	}
}
