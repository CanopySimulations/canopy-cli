namespace Canopy.Cli.Executable.Commands
{
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using Canopy.Api.Client;
    using Canopy.Cli.Executable.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System.IO;
    using System.Threading.Tasks;

    public class ProcessStudyResultsCommand : CanopyCommandBase
    {
        public record Parameters(
            DirectoryInfo Target,
            bool KeepOriginal);

        public override Command Create()
        {
            var command = new Command("process-study-results", "Creates user friendly files from raw study results.");

            command.AddOption(new Option<DirectoryInfo>(
                new[] { "--target", "-t" },
                description: $"The folder to process. The current directory is used if omitted.",
                getDefaultValue: () => new DirectoryInfo("./")));

            command.AddOption(new Option<bool>(
                new[] { "--keep-original", "-ko" },
                description: $"Do not delete files which have been processed (faster).",
                getDefaultValue: () => false));

            command.Handler = CommandHandler.Create((IHost host, Parameters parameters) =>
                host.Services.GetRequiredService<CommandRunner>().ExecuteAsync(parameters));

            return command;
        }

        public class CommandRunner
        {
            private readonly IEnsureAuthenticated ensureAuthenticated;
            private readonly IProcessLocalStudyResults processLocalStudyResults;

            public CommandRunner(
                IEnsureAuthenticated ensureAuthenticated,
                IProcessLocalStudyResults processLocalStudyResults)
            {
                this.ensureAuthenticated = ensureAuthenticated;
                this.processLocalStudyResults = processLocalStudyResults;
            }

            public async Task ExecuteAsync(Parameters parameters)
            {
                await this.ensureAuthenticated.ExecuteAsync();

                if (!parameters.Target.Exists)
                {
                    throw new RecoverableException("Folder not found: " + parameters.Target.FullName);
                }

                await this.processLocalStudyResults.ExecuteAsync(parameters.Target.FullName, !parameters.KeepOriginal);
            }
        }
    }
}