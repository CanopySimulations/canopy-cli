using System;
using System.Text;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Microsoft.Extensions.CommandLineUtils;

namespace Canopy.Cli.Executable.Commands
{
    public class AuthenticateCommand : CanopyCommandBase
    {
        private readonly CommandOption usernameOption;
        private readonly CommandOption companyOption;
        private readonly CommandOption passwordOption;
		
        public AuthenticateCommand()
        {
            this.RequiresAuthentication = false;

			this.Name = "authenticate";
			this.Description = "Authenticates with the API.";

            this.usernameOption = this.Option(
                "-u | --username",
                $"Your username.",
                CommandOptionType.SingleValue);

			this.companyOption = this.Option(
				"-c | --company",
				$"Your company.",
				CommandOptionType.SingleValue);

			this.passwordOption = this.Option(
				"-p | --password",
				$"Your password.",
				CommandOptionType.SingleValue);
		}

        protected override async Task ExecuteAsync()
        {
            var username = this.usernameOption.ValueOrPrompt("Username: ", "Username is required.");
            var company = this.companyOption.ValueOrPrompt("Company: ", "Company is required.");
            var password = this.passwordOption.ValueOrPrompt("Password: ", "Password is required.", true);

			AuthenticationManager.Instance.SetAuthenticationInformation(username, company, password);
            this.authenticatedUser = await AuthenticationManager.Instance.GetAuthenticatedUser();

            await this.TestAuthenticated();
		}

        private async Task TestAuthenticated()
        {
			var configClient = new ConfigClient(this.configuration);
			var result = await configClient.GetConfigsAsyncAsync(this.authenticatedUser.TenantId, "car", null, null);
		}

    }
}
