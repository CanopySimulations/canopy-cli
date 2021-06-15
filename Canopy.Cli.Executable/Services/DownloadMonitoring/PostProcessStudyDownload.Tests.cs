using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class PostProcessStudyDownloadTests
    {
        const string WindowsSuccessfulCommand = "cmd";
        const string WindowsSuccessfulCommandArguments = "/c echo running& echo {0}";

        const string LinuxSuccessfulCommand = "bash";
        const string LinuxSuccessfulCommandArguments = "-c \"echo running && echo {0}\"";

        const string WindowsFailingCommand = "cmd";
        const string WindowsFailingCommandArguments = "/c echo running& exit 1";

        const string LinuxFailingCommand = "bash";
        const string LinuxFailingCommandArguments = "-c \"echo running && exit 1\"";

        const string TargetFolderString = "target_folder";

        private readonly ILogger<PostProcessStudyDownload> logger;
        private readonly ILogPostProcessorOutput logPostProcessorOutput;

        private readonly PostProcessStudyDownload target;
        
        public PostProcessStudyDownloadTests()
        {
            this.logger = Substitute.For<ILogger<PostProcessStudyDownload>>();
            this.logPostProcessorOutput = Substitute.For<ILogPostProcessorOutput>();

            this.target = new PostProcessStudyDownload(
                this.logger,
                this.logPostProcessorOutput);
        }

        [Fact]
        public async Task WhenProcessSucceeds()
        {
            var (command, args) = GetSuccessfulCommand();

            await this.target.ExecuteAsync(command, args, TargetFolderString);

            this.logPostProcessorOutput.Received(1).Information(
                PostProcessStudyDownload.PostProcessorOutputFormat,
                command,
                "running");

            this.logPostProcessorOutput.Received(1).Information(
                PostProcessStudyDownload.PostProcessorOutputFormat, 
                command, 
                "\"target_folder\"");

            this.logPostProcessorOutput.Received(0).Error(Arg.Any<string>(), Arg.Any<object[]>());
        }

        [Fact]
        public async Task WhenProcessFails()
        {
            var (command, args) = GetFailingCommand();

            await this.target.ExecuteAsync(command, args, TargetFolderString);

            this.logPostProcessorOutput.Received(1).Information(
                PostProcessStudyDownload.PostProcessorOutputFormat,
                command,
                "running");

            this.logPostProcessorOutput.Received(1).Error(Arg.Any<string>(), Arg.Any<object[]>());
        }

        public (string, string) GetSuccessfulCommand()
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return (WindowsSuccessfulCommand, WindowsSuccessfulCommandArguments);
            }

            return (LinuxSuccessfulCommand, LinuxSuccessfulCommandArguments);
        }

        
        public (string, string) GetFailingCommand()
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return (WindowsFailingCommand, WindowsFailingCommandArguments);
            }

            return (LinuxFailingCommand, LinuxFailingCommandArguments);
        }
    }
}