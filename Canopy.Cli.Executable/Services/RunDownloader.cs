using System.Threading.Tasks;
using Canopy.Cli.Executable.Commands;

namespace Canopy.Cli.Executable.Services
{
    public class RunDownloader : IRunDownloader
    {
        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly IMonitorDownloads monitorDownloads;

        public RunDownloader(
            IEnsureAuthenticated ensureAuthenticated,
            IMonitorDownloads monitorDownloads)
        {
            this.ensureAuthenticated = ensureAuthenticated;
            this.monitorDownloads = monitorDownloads;
        }

        public async Task ExecuteAsync(DownloaderCommand.Parameters parameters)
        {
            await this.ensureAuthenticated.ExecuteAsync();

            var inputFolder = Utilities.GetCreatedOutputFolder(parameters.InputFolder);
            var outputFolder = Utilities.GetCreatedOutputFolder(parameters.OutputFolder);

            using var cts = CommandUtilities.CreateCommandCancellationTokenSource();

            await this.monitorDownloads.ExecuteAsync(
                inputFolder,
                outputFolder,
                parameters.KeepBinary,
                parameters.GenerateCsv);
        }
    }
}