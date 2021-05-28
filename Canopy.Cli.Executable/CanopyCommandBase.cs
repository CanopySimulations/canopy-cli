using System.CommandLine;
using System.Threading.Tasks;
using Canopy.Api.Client;

namespace Canopy.Cli.Executable
{
    public abstract class CanopyCommandBase
    {
		public CanopyCommandBase()
        {
        }

        public abstract Command Create();
    }
}
