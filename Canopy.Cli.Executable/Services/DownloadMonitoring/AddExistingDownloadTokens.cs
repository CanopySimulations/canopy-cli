using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class AddExistingDownloadTokens : IAddExistingDownloadTokens
    {
        private readonly IGetDownloadTokens getDownloadTokens;
        private readonly ITryAddDownloadToken tryAddDownloadToken;
        private readonly ILogger<AddExistingDownloadTokens> logger;

        public AddExistingDownloadTokens(
            IGetDownloadTokens getDownloadTokens,
            ITryAddDownloadToken tryAddDownloadToken,
            ILogger<AddExistingDownloadTokens> logger)
        {
            this.getDownloadTokens = getDownloadTokens;
            this.tryAddDownloadToken = tryAddDownloadToken;
            this.logger = logger;
        }

        public async Task ExecuteAsync(
            ChannelWriter<QueuedDownloadToken> channelWriter,
            string folderPath,
            CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Looking for existing download tokens in {0}", folderPath);
            foreach (var filePath in this.getDownloadTokens.Execute(folderPath))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await this.tryAddDownloadToken.ExecuteAsync(channelWriter, filePath, cancellationToken);
            }
        }
    }
}