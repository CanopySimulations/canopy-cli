namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IGetAvailableOutputFolder
    {
        string Execute(string desiredOutputFolderPath);
    }
}