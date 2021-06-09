namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class DownloaderConstants
    {
        public const string DownloadTokenExtension = "canopy-download-token";
        public const string DownloadTokenExtensionWithPeriod = "." + DownloadTokenExtension;
        public const string CompletedDownloadTokenFileName = "completed-download" + DownloadTokenExtensionWithPeriod;
    }
}