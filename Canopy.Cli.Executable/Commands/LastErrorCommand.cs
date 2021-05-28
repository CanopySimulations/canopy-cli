using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Canopy.Cli.Executable.Commands
{
    class LastErrorCommand : CanopyCommandBase
    {
        public override Command Create()
        {
            var command = new Command("last-error", "Shows the last logged error.");

            command.Handler = CommandHandler.Create((IHost host) =>
                host.Services.GetRequiredService<CommandRunner>().ExecuteAsync());

            return command;
        }

        public class CommandRunner
        {
            public Task ExecuteAsync()
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(Utilities.ReadError());
                Console.ResetColor();
                return Task.CompletedTask;
            }
        }
    }
}
