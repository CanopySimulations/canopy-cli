using System;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Canopy.Cli.Executable.Commands
{
    class LastErrorCommand : CanopyCommandBase
    {
        public override Command Create(IHost host)
        {
            var command = new Command("last-error", "Shows the last logged error.");

            command.SetAction((ParseResult parseResult) =>
                host.Services.GetRequiredService<CommandRunner>().Execute());

            return command;
        }

        public class CommandRunner
        {
            public void Execute()
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(Utilities.ReadError());
                Console.ResetColor();
            }
        }
    }
}
