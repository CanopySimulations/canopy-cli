using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Commands
{
    class LastErrorCommand : CanopyCommandBase
    {
        public LastErrorCommand()
        {
            this.RequiresConnection = false;
            this.RequiresAuthentication = false;

            this.Name = "last-error";
            this.Description = "Shows the last logged error.";
        }

        protected override Task<int> ExecuteAsync()
        {
            Console.WriteLine("Last Error:");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(Utilities.ReadError());
            Console.ResetColor();
            return Task.FromResult(0);
        }
    }
}
