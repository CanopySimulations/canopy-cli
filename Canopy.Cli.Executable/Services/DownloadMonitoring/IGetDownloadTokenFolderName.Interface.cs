namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface IGetDownloadTokenFolderName
    {
        string Execute(QueuedDownloadToken item);
    }
}