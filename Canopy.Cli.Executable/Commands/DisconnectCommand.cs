using System;
using System.CommandLine;
using Canopy.Api.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Commands
{
    public class DisconnectCommand : CanopyCommandBase
    {
        public override Command Create(IHost host)
        {
            var command = new Command("disconnect", "Disconnects from the API endpoint, and removes any authentication information.");

            command.SetAction((ParseResult parseResult) =>
                host.Services.GetRequiredService<CommandRunner>().Execute());

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

            public void Execute()
            {
                this.authenticationManager.ClearAuthenticatedUser();
                this.connectionManager.ClearConnectionInformation();

                this.logger.LogInformation("Disconnected.");
            }
        }
    }
}
