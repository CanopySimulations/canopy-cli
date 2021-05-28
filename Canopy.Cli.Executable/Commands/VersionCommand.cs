using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Commands
{
    public class VersionCommand : CanopyCommandBase
    {
        public override Command Create()
        {
            var command = new Command("version", "Displays the current canopy-cli version.");
            command.Handler = CommandHandler.Create((IHost host) => this.ExecuteAsync(host));
            return command;
        }
        
        private Task ExecuteAsync(IHost host)
        {
            host.Services.GetRequiredService<CommandRunner>().Execute();
            
            return Task.CompletedTask;
        }

        public class CommandRunner
        {
            private readonly ILogger<CommandRunner> logger;

            public CommandRunner(
                ILogger<CommandRunner> logger)
            {
                this.logger = logger;
            }

            public void Execute()
            {
                var entryAssembly = Assembly.GetEntryAssembly();

                Guard.Operation(entryAssembly != null, "Entry assembly not found.");

                this.logger.LogInformation(
                    "Canopy CLI version is {0}", entryAssembly.GetName().Version);
            }
        }
    }
}
