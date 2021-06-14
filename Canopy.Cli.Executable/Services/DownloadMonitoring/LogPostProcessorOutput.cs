using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class LogPostProcessorOutput : ILogPostProcessorOutput
    {
        private readonly ILogger<LogPostProcessorOutput> logger;

        public LogPostProcessorOutput(ILogger<LogPostProcessorOutput> logger)
        {
            this.logger = logger;
        }

        public void Information(string message, params object[] args)
        {
            this.logger.LogInformation(message, args);
        }

        public void Error(string message, params object[] args)
        {
            this.logger.LogError(message, args);
        }
    }
}
