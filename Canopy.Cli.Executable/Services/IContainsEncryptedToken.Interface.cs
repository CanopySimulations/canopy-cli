using Newtonsoft.Json.Linq;

namespace Canopy.Cli.Executable.Services
{
    public interface IContainsEncryptedToken
    {
        bool Execute(string content);
        bool Execute(JObject content);
    }
}