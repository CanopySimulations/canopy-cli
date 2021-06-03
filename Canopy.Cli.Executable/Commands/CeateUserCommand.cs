using System.CommandLine;
using System.CommandLine.Invocation;
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

        public override Command Create()
        {
            var command = new Command("create-user", "Creates a new user for the current tenant.");

            command.AddOption(new Option<string>(
                new[] { "--username", "-u" },
                description: "New username.",
                getDefaultValue: () => string.Empty));

            command.AddOption(new Option<string>(
                new[] { "--email", "-e" },
                description: "User's email address.",
                getDefaultValue: () => string.Empty));

            command.AddOption(new Option<string>(
                new[] { "--password", "-p" },
                description: "User's password.",
                getDefaultValue: () => string.Empty));

            command.Handler = CommandHandler.Create((IHost host, Parameters parameters) =>
                host.Services.GetRequiredService<CommandRunner>().ExecuteAsync(
                    parameters with
                    {
                        Username = CommandUtilities.ValueOrPrompt(parameters.Username, "Username: ", "Username is required.", false),
                        Email = CommandUtilities.ValueOrPrompt(parameters.Email, "Email: ", "Email is required.", false),
                        Password = CommandUtilities.ValueOrPrompt(parameters.Password, "Password: ", "Password is required.", true),
                    }));

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

            public async Task ExecuteAsync(Parameters parameters)
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
