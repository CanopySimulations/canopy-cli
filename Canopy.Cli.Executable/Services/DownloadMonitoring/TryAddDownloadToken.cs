using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class TryAddDownloadToken : ITryAddDownloadToken
    {
        private readonly IAddedDownloadTokensCache addedDownloadTokensCache;
        private readonly IReadDownloadToken readDownloadToken;
        private readonly ILogger<TryAddDownloadToken> logger;

        public TryAddDownloadToken(
            IAddedDownloadTokensCache addedDownloadTokensCache,
            IReadDownloadToken readDownloadToken,
            ILogger<TryAddDownloadToken> logger)
        {
            this.readDownloadToken = readDownloadToken;
            this.logger = logger;
            this.addedDownloadTokensCache = addedDownloadTokensCache;
        }

        public async Task ExecuteAsync(ChannelWriter<QueuedDownloadToken> channelWriter, string filePath, CancellationToken cancellationToken)
        {
            try
            {
                if (this.addedDownloadTokensCache.TryAdd(filePath))
                {
                    var token = await this.readDownloadToken.ExecuteAsync(filePath, cancellationToken);
                    this.logger.LogInformation("Enqueing {0}", Path.GetFileName(filePath));
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