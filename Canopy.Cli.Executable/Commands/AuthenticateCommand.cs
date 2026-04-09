using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Canopy.Cli.Executable.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Canopy.Cli.Executable.Commands
{
    public class AuthenticateCommand : CanopyCommandBase
    {
        public record Parameters(
            string Username,
            string Company,
            string Password);

        public override Command Create(IHost host)
        {
            var command = new Command("authenticate", "Authenticates with the API.");

            var username = command.AddOption("--username", "-u", string.Empty, "Your username.");
            var company = command.AddOption("--company", "-c", string.Empty, "Your company.");
            var password = command.AddOption("--password", "-p", string.Empty, "Your password.");

            command.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
            {
                var parameters = new Parameters(
                    parseResult.GetValue(username),
                    parseResult.GetValue(company),
                    parseResult.GetValue(password));
                return host.Services.GetRequiredService<CommandRunner>().ExecuteAsync(
                    parameters with
                    {
                        Username = CommandUtilities.ValueOrPrompt(parameters.Username, "Username: ", "Username is required.", false),
                        Company = CommandUtilities.ValueOrPrompt(parameters.Company, "Company: ", "Company is required.", false),
                        Password = CommandUtilities.ValueOrPrompt(parameters.Password, "Password: ", "Password is required.", true),
                    },
                    cancellationToken);
            });

            return command;
        }

        public class CommandRunner
        {
            private readonly IEnsureConnected ensureConnected;
            private readonly IAuthenticationManager authenticationManager;
            private readonly IConfigClient configClient;

            public CommandRunner(
                IEnsureConnected ensureConnected,
                IAuthenticationManager authenticationManager,
                IConfigClient configClient)
            {
                this.authenticationManager = authenticationManager;
                this.configClient = configClient;
                this.ensureConnected = ensureConnected;
            }

            public async Task ExecuteAsync(Parameters parameters, CancellationToken cancellationToken)
            {
                this.ensureConnected.Execute();

                this.authenticationManager.SetAuthenticationInformation(
                    parameters.Username,
                    parameters.Company,
                    parameters.Password);

                var authenticatedUser = await this.authenticationManager.GetAuthenticatedUser();

                await this.TestAuthenticated(authenticatedUser);
            }

            private async Task TestAuthenticated(AuthenticatedUser authenticatedUser)
            {
                var result = await this.configClient.GetConfigsAsync(authenticatedUser.TenantId, "car", null, null, null);
            }
        }
    }
}
