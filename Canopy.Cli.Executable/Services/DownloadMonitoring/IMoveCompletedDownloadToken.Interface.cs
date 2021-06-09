namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IMoveCompletedDownloadToken
    {
        void Execute(string tokenPath, string outputFolder);
    }
}