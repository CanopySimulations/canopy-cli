using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class WatchForNewDownloadTokens : IWatchForNewDownloadTokens
    {
        private readonly ITryAddDownloadToken tryAddDownloadToken;
        private readonly ILogger<WatchForNewDownloadTokens> logger;

        public WatchForNewDownloadTokens(
            ITryAddDownloadToken tryAddDownloadToken,
            ILogger<WatchForNewDownloadTokens> logger)
        {
            this.logger = logger;
            this.tryAddDownloadToken = tryAddDownloadToken;
        }

        public Task ExecuteAsync(
            ChannelWriter<QueuedDownloadToken> channelWriter,
            string folderPath,
            CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource();
            var watcher = new FileSystemWatcher(folderPath);

            cancellationToken.Register(() =>
            {
                watcher.Dispose();
                tcs.TrySetResult();
            });

            watcher.NotifyFilter = NotifyFilters.FileName;

            watcher.Error += (s, e) => this.logger.LogError(e.GetException(), "Unable to monitor file system folder: {0}", folderPath);
            watcher.Created += (s, e) => this.ProcessNewFile(e.FullPath, channelWriter, cancellationToken);
            watcher.Changed += (s, e) => this.ProcessNewFile(e.FullPath, channelWriter, cancellationToken);
            watcher.Renamed += (s, e) => this.ProcessNewFile(e.FullPath, channelWriter, cancellationToken);

            watcher.Filter = $"*{DownloaderConstants.DownloadTokenExtensionWithPeriod}";

            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            return tcs.Task;
        }

        private async void ProcessNewFile(string fullPath, ChannelWriter<QueuedDownloadToken> channelWriter, CancellationToken cancellationToken)
        {
            try
            {
                if (Path.GetExtension(fullPath) != DownloaderConstants.DownloadTokenExtensionWithPeriod)
                {
                    return;
                }

                if (Path.GetFileName(fullPath) == DownloaderConstants.CompletedDownloadTokenFileName)
                {
                    return;
                }

                await this.tryAddDownloadToken.ExecuteAsync(channelWriter, fullPath, cancellationToken);
            }
            catch (Exception t)
            {
                this.logger.LogWarning(t, "Failed to process path: {0}", fullPath);
            }
        }
    }
}