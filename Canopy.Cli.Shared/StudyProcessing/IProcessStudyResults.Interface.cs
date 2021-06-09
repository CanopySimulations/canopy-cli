namespace Canopy.Cli.Shared.StudyProcessing
{
    using System.Threading.Tasks;

    public interface IProcessStudyResults
    {
        Task ExecuteAsync(
            IRootFolder root,
            IFileWriter writer,
            bool channelsAsCsv,
            bool deleteProcessedFiles,
            int parallelism);
    }
}