using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface ITryAddDownloadToken
    {
        Task ExecuteAsync(ChannelWriter<QueuedDownloadToken> channelWriter, string filePath, CancellationToken cancellationToken);
    }
}