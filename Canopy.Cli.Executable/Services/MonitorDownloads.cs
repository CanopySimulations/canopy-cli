using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public class MonitorDownloads : IMonitorDownloads
    {
        public Task ExecuteAsync(
            string inputFolder,
            string outputFolder,
            bool deleteProcessedFiles,
            bool generateCsvFiles)
        {
            return Task.CompletedTask;
        }
    }
}