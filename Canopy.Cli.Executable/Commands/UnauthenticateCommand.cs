using System;
using System.CommandLine;
using Canopy.Api.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Canopy.Cli.Executable.Commands
{
    public class UnauthenticateCommand : CanopyCommandBase
    {
        public override Command Create(IHost host)
        {
            var command = new Command("unauthenticate", "Unauthenticates from the API and removes local sign-in information.");

            command.SetAction((ParseResult parseResult) =>
                host.Services.GetRequiredService<CommandRunner>().Execute());

            return command;
        }

        public class CommandRunner
        {
            private readonly IAuthenticationManager authenticationManager;
            
            public CommandRunner(IAuthenticationManager authenticationManager)
            {
                this.authenticationManager = authenticationManager;
            }

            public void Execute()
            {
                this.authenticationManager.ClearAuthenticatedUser();
            }
        }
    }
}
