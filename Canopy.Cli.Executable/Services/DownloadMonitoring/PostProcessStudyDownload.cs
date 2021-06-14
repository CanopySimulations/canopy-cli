using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class PostProcessStudyDownload : IPostProcessStudyDownload
    {
        public const string PostProcessorOutputPrefix = "[{0}]: ";
        public const string PostProcessorOutputFormat = PostProcessorOutputPrefix + "{1}";

        private readonly ILogger<PostProcessStudyDownload> logger;
        private readonly ILogPostProcessorOutput logPostProcessorOutput;

        public PostProcessStudyDownload(
            ILogger<PostProcessStudyDownload> logger,
            ILogPostProcessorOutput logPostProcessorOutput)
        {
            this.logger = logger;
            this.logPostProcessorOutput = logPostProcessorOutput;
        }

        public async Task ExecuteAsync(
            string postProcessorPath,
            string postProcessorArguments,
            string targetFolder)
        {
            try
            {
                await this.ExecuteInnerAsync(postProcessorPath, postProcessorArguments, targetFolder);
            }
            catch (Exception t)
            {
                this.logger.LogError(t, "Error running post-processor.");
            }
        }

        private async Task ExecuteInnerAsync(
            string postProcessorPath,
            string postProcessorArguments,
            string targetFolder)
        {
            if (string.IsNullOrWhiteSpace(postProcessorArguments))
            {
                postProcessorArguments = "\"{0}\"";
            }

            var resolvedArguments = string.Format(postProcessorArguments, targetFolder);

            var psi = new ProcessStartInfo(postProcessorPath, resolvedArguments);
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;

            var postProcessorName = Path.GetFileName(postProcessorPath);

            this.logger.LogInformation(postProcessorName + " " + psi.Arguments);

            var process = Process.Start(psi);

            Guard.Operation(process != null, "Attempting to start {0} produced a null process.", postProcessorPath);

            process.OutputDataReceived += (s, e) => this.logPostProcessorOutput.Information(PostProcessorOutputFormat, postProcessorName, e.Data ?? string.Empty);
            process.ErrorDataReceived += (s, e) => 
            {
                if(!string.IsNullOrWhiteSpace(e.Data))
                {
                    this.logPostProcessorOutput.Error(PostProcessorOutputFormat, postProcessorName, e.Data);
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                this.logPostProcessorOutput.Error(PostProcessorOutputPrefix + "Exit code {1} ({2})", postProcessorName, process.ExitCode, "0x" + process.ExitCode.ToString("X4"));
            }
        }
    }
}
