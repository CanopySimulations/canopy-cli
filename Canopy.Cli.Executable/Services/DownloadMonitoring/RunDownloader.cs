using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Canopy.Cli.Executable.Commands;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class RunDownloader : IRunDownloader
    {
        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly IMonitorDownloads monitorDownloads;
        private readonly IProcessDownloads processDownloads;
        private readonly IGetCreatedOutputFolder getCreatedOutputFolder;
        private readonly ILogger<RunDownloader> logger;

        public RunDownloader(
            IEnsureAuthenticated ensureAuthenticated,
            IGetCreatedOutputFolder getCreatedOutputFolder,
            IMonitorDownloads monitorDownloads,
            IProcessDownloads processDownloads,
            ILogger<RunDownloader> logger)
        {
            this.getCreatedOutputFolder = getCreatedOutputFolder;
            this.ensureAuthenticated = ensureAuthenticated;
            this.monitorDownloads = monitorDownloads;
            this.processDownloads = processDownloads;
            this.logger = logger;
        }

        public async Task ExecuteAsync(DownloadMonitorCommand.Parameters parameters)
        {
            await this.ensureAuthenticated.ExecuteAsync();

            var inputFolder = this.getCreatedOutputFolder.Execute(parameters.InputFolder);
            var outputFolder = this.getCreatedOutputFolder.Execute(parameters.OutputFolder);

            using var cts = CommandUtilities.CreateCommandCancellationTokenSource();

            var channel = Channel.CreateUnbounded<QueuedDownloadToken>();

            try
            {
                var monitorDownloadsTask = this.monitorDownloads.ExecuteAsync(
                    channel.Writer,
                    inputFolder,
                    cts.Token);

                await this.processDownloads.ExecuteAsync(
                    channel.Reader,
                    targetFolder: outputFolder,
                    generateCsv: parameters.GenerateCsv,
                    keepBinary: parameters.KeepBinary,
                    postProcessorPath: parameters.PostProcessor,
                    postProcessorArguments: parameters.PostProcessorArguments,
                    cancellationToken: cts.Token);

                await monitorDownloadsTask;
            }
            catch (Exception t) when (ExceptionUtilities.IsFromCancellation(t))
            {
                // Download monitoring was cancelled.
            }
        }
    }
}