using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class AddExistingDownloadTokens : IAddExistingDownloadTokens
    {
        private readonly IGetDownloadTokens getDownloadTokens;
        private readonly ITryAddDownloadToken tryAddDownloadToken;

        public AddExistingDownloadTokens(
            IGetDownloadTokens getDownloadTokens,
            ITryAddDownloadToken tryAddDownloadToken)
        {
            this.getDownloadTokens = getDownloadTokens;
            this.tryAddDownloadToken = tryAddDownloadToken;
        }

        public async Task ExecuteAsync(
            ChannelWriter<QueuedDownloadToken> channelWriter,
            string folderPath,
            CancellationToken cancellationToken)
        {
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