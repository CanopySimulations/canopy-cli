namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class GetDownloadTokenFolderName : IGetDownloadTokenFolderName
    {
        public string Execute(QueuedDownloadToken item)
        {
            var folderName = item.Token.StudyName;
            if (item.Token.Job != null)
            {
                folderName += " - " + item.Token.Job.JobName;
            }

            return FileNameUtilities.Sanitize(folderName);
        }
    }
}