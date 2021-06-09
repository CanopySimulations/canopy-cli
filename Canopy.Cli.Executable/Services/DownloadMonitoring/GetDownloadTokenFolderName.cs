namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class GetDownloadTokenFolderName : IGetDownloadTokenFolderName
    {
        public string Execute(QueuedDownloadToken item)
        {
            var folderName = item.Token.StudyName;
            if (!string.IsNullOrWhiteSpace(item.Token.JobName))
            {
                folderName += " - " + item.Token.JobName;
            }

            return folderName;
        }
    }
}