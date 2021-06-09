using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class TryAddDownloadToken : ITryAddDownloadToken
    {
        private readonly IAddedDownloadTokensCache addedDownloadTokensCache;
        private readonly IReadDownloadToken readDownloadToken;

        public TryAddDownloadToken(
            IAddedDownloadTokensCache addedDownloadTokensCache,
            IReadDownloadToken readDownloadToken)
        {
            this.readDownloadToken = readDownloadToken;
            this.addedDownloadTokensCache = addedDownloadTokensCache;
        }

        public async Task ExecuteAsync(ChannelWriter<QueuedDownloadToken> channelWriter, string filePath, CancellationToken cancellationToken)
        {
            try
            {
                if (this.addedDownloadTokensCache.TryAdd(filePath))
                {
                    var token = await this.readDownloadToken.ExecuteAsync(filePath, cancellationToken);
                    await channelWriter.WriteAsync(new QueuedDownloadToken(filePath, token), cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }
}