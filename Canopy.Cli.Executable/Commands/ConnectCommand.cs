using System;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Microsoft.Extensions.CommandLineUtils;

namespace Canopy.Cli.Executable.Commands
{
    public class ConnectCommand : CanopyCommandBase
    {
        private readonly CommandOption endpointOption;
        private readonly CommandOption clientIdOption;
        private readonly CommandOption clientSecretOption;

        public ConnectCommand()
        {
			this.RequiresConnection = false;
			this.RequiresAuthentication = false;

            this.Name = "connect";
            this.Description = "Connects to an API endpoint.";

            this.endpointOption = this.Option(
                "-e | --endpoint",
                $"The API endpoint to connect to (defaults to {ConnectionManager.DefaultApiEndpoint}).", 
                CommandOptionType.SingleValue);

			this.clientIdOption = this.Option(
				"-c | --client-id",
                $"Your client ID, as provided by Canopy Simulations.",
				CommandOptionType.SingleValue);
            
			this.clientSecretOption = this.Option(
				"-s | --client-secret",
				$"Your client secret, as provided by Canopy Simulations.",
				CommandOptionType.SingleValue);
		}

        protected override async Task<int> ExecuteAsync()
        {
			var endpoint = this.endpointOption.Value();
            if(string.IsNullOrWhiteSpace(endpoint))
            {
                endpoint = ConnectionManager.DefaultApiEndpoint;
            }

			var clientId = this.clientIdOption.ValueOrPrompt("Client ID: ", "Client ID is required.");
			var clientSecret = this.clientSecretOption.ValueOrPrompt("Client Secret: ", "Client Secret is required.", true);

            ConnectionManager.Instance.SetConnectionInformation(
                new ConnectionInformation(endpoint, clientId, clientSecret));

            var availabilityClient = new AvailabilityClient(this.configuration);
            await availabilityClient.GetAsync(false);
            return 0;
		}
    }
}
