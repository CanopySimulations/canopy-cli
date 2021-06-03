using System.Threading.Tasks;
using Canopy.Cli.Executable.Commands;

namespace Canopy.Cli.Executable.Services
{
    public class RunDownloader : IRunDownloader
    {
        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly IMonitorDownloads monitorDownloads;
        private readonly IGetCreatedOutputFolder getCreatedOutputFolder;

        public RunDownloader(
            IEnsureAuthenticated ensureAuthenticated,
            IGetCreatedOutputFolder getCreatedOutputFolder,
            IMonitorDownloads monitorDownloads)
        {
            this.getCreatedOutputFolder = getCreatedOutputFolder;
            this.ensureAuthenticated = ensureAuthenticated;
            this.monitorDownloads = monitorDownloads;
        }

        public async Task ExecuteAsync(DownloaderCommand.Parameters parameters)
        {
            await this.ensureAuthenticated.ExecuteAsync();

            var inputFolder = this.getCreatedOutputFolder.Execute(parameters.InputFolder);
            var outputFolder = this.getCreatedOutputFolder.Execute(parameters.OutputFolder);

            using var cts = CommandUtilities.CreateCommandCancellationTokenSource();

            await this.monitorDownloads.ExecuteAsync(
                inputFolder,
                outputFolder,
                parameters.KeepBinary,
                parameters.GenerateCsv);
        }
    }
}