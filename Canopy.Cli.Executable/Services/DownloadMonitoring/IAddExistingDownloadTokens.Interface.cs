using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IAddExistingDownloadTokens
    {
        Task ExecuteAsync(ChannelWriter<QueuedDownloadToken> channelWriter, string folderPath, CancellationToken cancellationToken);
    }
}