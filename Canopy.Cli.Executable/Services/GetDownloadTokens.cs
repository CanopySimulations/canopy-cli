using System.IO;
using System.Threading;
using System.Threading.Channels;

namespace Canopy.Cli.Executable.Services
{
    public record DownloadToken;

    public class GetDownloadTokens
    {
        public ChannelReader<DownloadToken> Execute(string folderPath, CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<DownloadToken>();
            var watcher = new FileSystemWatcher(folderPath);

            cancellationToken.Register(() => 
            {
                watcher.Dispose();
                channel.Writer.Complete();
            });

            watcher.Created += async (s, e) =>
            {
                var token = new DownloadToken();
                await channel.Writer.WriteAsync(token);
            };

            return channel.Reader;
        }
    }
}