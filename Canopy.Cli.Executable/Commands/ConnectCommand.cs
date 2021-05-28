using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Canopy.Cli.Executable.Commands
{
    public class ConnectCommand : CanopyCommandBase
    {
        public record Parameters(
            string Endpoint,
            string ClientId,
            string ClientSecret);

        public override Command Create()
        {
            var command = new Command("connect", "Connects to an API endpoint.");

            command.AddOption(new Option<string>(
                new[] { "--endpoint", "-e" },
                description: $"The API endpoint to connect to (defaults to {ConnectionManager.DefaultApiEndpoint}).",
                getDefaultValue: () => ConnectionManager.DefaultApiEndpoint));

            command.AddOption(new Option<string>(
                new[] { "--client-id", "-c" },
                description: $"Your client ID, as provided by Canopy Simulations.",
                getDefaultValue: () => string.Empty));

            command.AddOption(new Option<string>(
                new[] { "--client-secret", "-s" },
                description: $"Your client secret, as provided by Canopy Simulations.",
                getDefaultValue: () => string.Empty));

            command.Handler = CommandHandler.Create((IHost host, Parameters parameters) =>
                host.Services.GetRequiredService<CommandRunner>().ExecuteAsync(
                    parameters with
                    {
                        ClientId = CommandUtilities.ValueOrPrompt(parameters.ClientId, "Client ID: ", "Client ID is required.", false),
                        ClientSecret = CommandUtilities.ValueOrPrompt(parameters.ClientSecret, "Client Secret: ", "Client Secret is required.", true),
                    }));

            return command;
        }

        public class CommandRunner
        {
            private readonly IConnectionManager connectionManager;
            private readonly IAvailabilityClient availabilityClient;

            public CommandRunner(
                IConnectionManager connectionManager,
                IAvailabilityClient availabilityClient)
            {
                this.availabilityClient = availabilityClient;
                this.connectionManager = connectionManager;
            }

            public async Task ExecuteAsync(Parameters parameters)
            {
                this.connectionManager.SetConnectionInformation(
                    new ConnectionInformation(parameters.Endpoint, parameters.ClientId, parameters.ClientSecret));

                // Do a check to ensure we're connected.
                await this.availabilityClient.GetAsync(false);
            }
        }
    }
}
