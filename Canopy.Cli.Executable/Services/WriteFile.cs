using System.IO;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public class WriteFile : IWriteFile
    {
        public Task ExecuteAsync(string path, string content)
        {
            return File.WriteAllTextAsync(
                path,
                content);
        }
    }
}