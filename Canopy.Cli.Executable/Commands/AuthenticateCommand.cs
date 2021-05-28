using System.CommandLine;
using System.CommandLine.Invocation;
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

        public override Command Create()
        {
            var command = new Command("authenticate", "Authenticates with the API.");

            command.AddOption(new Option<string>(
                new[] { "--username", "-u" },
                description: "Your username.",
                getDefaultValue: () => string.Empty));

            command.AddOption(new Option<string>(
                new[] { "--company", "-c" },
                description: "Your company.",
                getDefaultValue: () => string.Empty));

            command.AddOption(new Option<string>(
                new[] { "--password", "-p" },
                description: "Your password.",
                getDefaultValue: () => string.Empty));

            command.Handler = CommandHandler.Create((IHost host, Parameters parameters) =>
                host.Services.GetRequiredService<CommandRunner>().ExecuteAsync(
                    parameters with
                    {
                        Username = CommandUtilities.ValueOrPrompt(parameters.Username, "Username: ", "Username is required.", false),
                        Company = CommandUtilities.ValueOrPrompt(parameters.Company, "Company: ", "Company is required.", false),
                        Password = CommandUtilities.ValueOrPrompt(parameters.Password, "Password: ", "Password is required.", true),
                    }));

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

            public async Task ExecuteAsync(Parameters parameters)
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
