using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Commands
{
    public class VersionCommand : CanopyCommandBase
    {
        public VersionCommand()
        {
            this.RequiresConnection = false;
            this.RequiresAuthentication = false;

            this.Name = "version";
            this.Description = "Displays the current canopy-cli version.";
        }

        protected override Task ExecuteAsync()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(Assembly.GetEntryAssembly().GetName().Version);
            Console.ResetColor();
            return Task.CompletedTask;
        }
    }
}
