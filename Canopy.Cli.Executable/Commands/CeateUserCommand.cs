using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Canopy.Cli.Executable.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Commands
{
    public class CeateUserCommand : CanopyCommandBase
    {
        public record Parameters(
            string Username,
            string Email,
            string Password);

        public override Command Create(IHost host)
        {
            var command = new Command("create-user", "Creates a new user for the current tenant.");

            var username = command.AddOption("--username", "-u", string.Empty, "New username.");
            var email = command.AddOption("--email", "-e", string.Empty, "User's email address.");
            var password = command.AddOption("--password", "-p", string.Empty, "User's password.");

            command.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
            {
                var parameters = new Parameters(
                    parseResult.GetValue(username),
                    parseResult.GetValue(email),
                    parseResult.GetValue(password));
                return host.Services.GetRequiredService<CommandRunner>().ExecuteAsync(
                    parameters with
                    {
                        Username = CommandUtilities.ValueOrPrompt(parameters.Username, "Username: ", "Username is required.", false),
                        Email = CommandUtilities.ValueOrPrompt(parameters.Email, "Email: ", "Email is required.", false),
                        Password = CommandUtilities.ValueOrPrompt(parameters.Password, "Password: ", "Password is required.", true),
                    },
                    cancellationToken);
            });

            return command;
        }

        public class CommandRunner
        {
            private readonly IEnsureAuthenticated ensureAuthenticated;
            private readonly IMembershipClient membershipClient;
            private readonly ILogger<CommandRunner> logger;

            public CommandRunner(
                IEnsureAuthenticated ensureAuthenticated,
                IMembershipClient membershipClient,
                ILogger<CommandRunner> logger)
            {
                this.ensureAuthenticated = ensureAuthenticated;
                this.membershipClient = membershipClient;
                this.logger = logger;
            }

            public async Task ExecuteAsync(Parameters parameters, CancellationToken cancellationToken)
            {
                var authenticatedUser = await this.ensureAuthenticated.ExecuteAsync();

                this.logger.LogInformation("Creating user {0}...", parameters.Username);
                await this.membershipClient.PostRegistrationAsync(new RegistrationData
                {
                    TenantId = authenticatedUser.TenantId,
                    Username = parameters.Username,
                    Email = parameters.Email,
                    Password = parameters.Password,
                });
                this.logger.LogInformation("User {0} created.", parameters.Username);
            }
        }
    }
}
