using System.IO;

namespace Canopy.Cli.Executable.Services
{
    public interface IGetCreatedOutputFolder
    {
        string Execute(DirectoryInfo folder);
        string Execute(string path);
    }
}