using System.IO;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class MoveCompletedDownloadToken : IMoveCompletedDownloadToken
    {
        public void Execute(string tokenPath, string outputFolder)
        {
            File.Move(tokenPath, Path.Combine(outputFolder, DownloaderConstants.CompletedDownloadTokenFileName));
        }
    }
}