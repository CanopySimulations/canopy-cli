using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class ReadDownloadToken : IReadDownloadToken
    {
        public async Task<DownloadToken> ExecuteAsync(string filePath, CancellationToken cancellationToken)
        {
            var tokenContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            return JsonConvert.DeserializeObject<DownloadToken>(tokenContent);
        }
    }
}