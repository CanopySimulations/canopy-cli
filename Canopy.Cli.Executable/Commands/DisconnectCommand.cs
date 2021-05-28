using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Commands
{
    public class DisconnectCommand : CanopyCommandBase
    {
        public override Command Create()
        {
            var command = new Command("disconnect", "Disconnects from the API endpoint, and removes any authentication information.");

            command.Handler = CommandHandler.Create((IHost host) =>
                host.Services.GetRequiredService<CommandRunner>().ExecuteAsync());

            return command;
        }

        public class CommandRunner
        {
            private readonly ILogger<CommandRunner> logger;
            private readonly IAuthenticationManager authenticationManager;
            private readonly IConnectionManager connectionManager;

            public CommandRunner(
                IAuthenticationManager authenticationManager,
                IConnectionManager connectionManager,
                ILogger<CommandRunner> logger)
            {
                this.connectionManager = connectionManager;
                this.authenticationManager = authenticationManager;
                this.logger = logger;
            }

            public Task ExecuteAsync()
            {
                this.authenticationManager.ClearAuthenticatedUser();
                this.connectionManager.ClearConnectionInformation();

                this.logger.LogInformation("Disconnected.");

                return Task.CompletedTask;
            }
        }
    }
}
