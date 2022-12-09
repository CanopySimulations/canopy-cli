using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IProcessDownloads
    {
        Task ExecuteAsync(
            ChannelReader<QueuedDownloadToken> channelReader,
            string targetFolder,
            bool generateCsv,
            bool keepBinary,
            PostProcessingParameters postProcessingParameters,
            CancellationToken cancellationToken);
    }
}