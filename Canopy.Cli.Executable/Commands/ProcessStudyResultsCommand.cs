namespace Canopy.Cli.Executable.Commands
{
    using System.CommandLine;
    using System.Threading;
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

        public override Command Create(IHost host)
        {
            var command = new Command("process-study-results", "Creates user friendly files from raw study results.");

            var target = command.AddOption("--target", "-t", new DirectoryInfo("./"), "The folder to process. The current directory is used if omitted.");
            var keepOriginal = command.AddOption("--keep-original", "-ko", false, "Do not delete files which have been processed (faster).");

            command.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
            {
                var parameters = new Parameters(
                    parseResult.GetValue(target),
                    parseResult.GetValue(keepOriginal));
                return host.Services.GetRequiredService<CommandRunner>().ExecuteAsync(parameters, cancellationToken);
            });

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

            public async Task ExecuteAsync(Parameters parameters, CancellationToken cancellationToken)
            {
                await this.ensureAuthenticated.ExecuteAsync();

                if (!parameters.Target.Exists)
                {
                    throw new RecoverableException("Folder not found: " + parameters.Target.FullName);
                }

                await this.processLocalStudyResults.ExecuteAsync(parameters.Target.FullName, !parameters.KeepOriginal, channelsAsCsv: true, channelsAsBinary: false, cancellationToken);
            }
        }
    }
}