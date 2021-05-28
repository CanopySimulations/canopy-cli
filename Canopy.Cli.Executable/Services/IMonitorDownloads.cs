using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public interface IMonitorDownloads
    {
        Task ExecuteAsync(string inputFolder, string outputFolder, bool deleteProcessedFiles, bool generateCsvFiles);
    }
}