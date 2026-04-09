using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Commands
{
    public class ConnectCommand : CanopyCommandBase
    {
        public record Parameters(
            string Endpoint,
            string ClientId,
            string ClientSecret);

        public override Command Create(IHost host)
        {
            var command = new Command("connect", "Connects to an API endpoint.");

            var endpoint = command.AddOption("--endpoint", "-e", ConnectionManager.DefaultApiEndpoint, $"The API endpoint to connect to (defaults to {ConnectionManager.DefaultApiEndpoint}).");
            var clientId = command.AddOption("--client-id", "-c", string.Empty, "Your client ID, as provided by Canopy Simulations.");
            var clientSecret = command.AddOption("--client-secret", "-s", string.Empty, "Your client secret, as provided by Canopy Simulations.");

            command.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
            {
                var parameters = new Parameters(
                    parseResult.GetValue(endpoint),
                    parseResult.GetValue(clientId),
                    parseResult.GetValue(clientSecret));
                return host.Services.GetRequiredService<CommandRunner>().ExecuteAsync(
                    parameters with
                    {
                        ClientId = CommandUtilities.ValueOrPrompt(parameters.ClientId, "Client ID: ", "Client ID is required.", false),
                        ClientSecret = CommandUtilities.ValueOrPrompt(parameters.ClientSecret, "Client Secret: ", "Client Secret is required.", true),
                    },
                    cancellationToken);
            });

            return command;
        }

        public class CommandRunner
        {
            private readonly IConnectionManager connectionManager;
            private readonly IAvailabilityClient availabilityClient;
            private readonly ILogger<CommandRunner> logger;

            public CommandRunner(
                IConnectionManager connectionManager,
                IAvailabilityClient availabilityClient,
                ILogger<CommandRunner> logger)
            {
                this.availabilityClient = availabilityClient;
                this.logger = logger;
                this.connectionManager = connectionManager;
            }

            public async Task ExecuteAsync(Parameters parameters, CancellationToken cancellationToken)
            {
                this.connectionManager.SetConnectionInformation(
                    new ConnectionInformation(parameters.Endpoint, parameters.ClientId, parameters.ClientSecret));

                // Do a check to ensure we're connected.
                await this.availabilityClient.GetAsync(false, true);

                this.logger.LogInformation("Connected.");
            }
        }
    }
}
