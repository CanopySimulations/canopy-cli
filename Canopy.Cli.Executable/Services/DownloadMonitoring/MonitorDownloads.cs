using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class MonitorDownloads : IMonitorDownloads
    {
        private readonly IAddExistingDownloadTokens addExistingDownloadTokens;
        private readonly IWatchForNewDownloadTokens watchForNewDownloadTokens;

        public MonitorDownloads(
            IAddExistingDownloadTokens addExistingDownloadTokens,
            IWatchForNewDownloadTokens watchForNewDownloadTokens)
        {
            this.watchForNewDownloadTokens = watchForNewDownloadTokens;
            this.addExistingDownloadTokens = addExistingDownloadTokens;
        }

        public async Task ExecuteAsync(
            ChannelWriter<QueuedDownloadToken> channelWriter,
            string inputFolder,
            CancellationToken cancellationToken)
        {

            var watchTask = this.watchForNewDownloadTokens.ExecuteAsync(
                channelWriter,
                inputFolder,
                cancellationToken);

            var addExistingTask = this.addExistingDownloadTokens.ExecuteAsync(
                channelWriter,
                inputFolder,
                cancellationToken);

            cancellationToken.Register(() =>
            {
                channelWriter.Complete();
            });

            await addExistingTask;
            await watchTask;
        }
    }
}