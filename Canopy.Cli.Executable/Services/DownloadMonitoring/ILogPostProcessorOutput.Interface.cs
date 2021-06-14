namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public interface ILogPostProcessorOutput
    {
        void Error(string message, params object[] args);
        void Information(string message, params object[] args);
    }
}
