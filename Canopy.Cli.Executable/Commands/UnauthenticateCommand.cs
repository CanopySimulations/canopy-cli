using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Canopy.Cli.Executable.Commands
{
    public class UnauthenticateCommand : CanopyCommandBase
    {
        public override Command Create()
        {
            var command = new Command("unauthenticate", "Unauthenticates from the API and removes local sign-in information.");

            command.Handler = CommandHandler.Create((IHost host) =>
                host.Services.GetRequiredService<CommandRunner>().ExecuteAsync());

            return command;
        }

        public class CommandRunner
        {
            private readonly IAuthenticationManager authenticationManager;
            
            public CommandRunner(IAuthenticationManager authenticationManager)
            {
                this.authenticationManager = authenticationManager;
            }

            public Task ExecuteAsync()
            {
                this.authenticationManager.ClearAuthenticatedUser();
                return Task.CompletedTask;
            }
        }
    }
}
