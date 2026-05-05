using System.CommandLine;
using Microsoft.Extensions.Hosting;

namespace Canopy.Cli.Executable
{
    public abstract class CanopyCommandBase
    {
        public abstract Command Create(IHost host);
    }
}
