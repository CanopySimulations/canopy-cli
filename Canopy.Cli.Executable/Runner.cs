using System;
using System.Linq;
using System.Reflection;
using Canopy.Cli.Executable.Commands;
using Microsoft.Extensions.CommandLineUtils;

namespace Canopy.Cli.Executable
{
    public class Runner : CommandLineApplication
    {
        public Runner()
        {
            var commands = from t in Assembly.GetEntryAssembly().GetTypes()
                           where typeof(CanopyCommandBase).IsAssignableFrom(t) && t != typeof(CanopyCommandBase)
                           select (CanopyCommandBase)Activator.CreateInstance(t);
                  
            this.Name = "canopy";

            foreach(var command in commands)
            {
                this.Commands.Add(command);
            }

			this.HelpOption("-h | -? | --help");

            this.OnExecute(() => {
				this.ShowHelp();
                return 0;
            });
		}
    }
}
