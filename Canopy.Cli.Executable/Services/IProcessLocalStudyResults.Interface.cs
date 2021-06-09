namespace Canopy.Cli.Executable.Services
{
    using System.Threading.Tasks;

    public interface IProcessLocalStudyResults
    {
        Task ExecuteAsync(string targetFolder, bool deleteProcessedFiles);
    }
}