using System.CommandLine;
using System.Threading;
using Canopy.Cli.Executable.Services;
using Canopy.Cli.Executable.Services.DownloadMonitoring;
using Canopy.Cli.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Canopy.Cli.Executable.Commands
{
    public class DownloadMonitorCommand : CanopyCommandBase
    {
        public record Parameters(
            string InputFolder,
            string OutputFolder,
            bool GenerateCsv,
            bool KeepBinary,
            string PostProcessor,
            string PostProcessorArguments,
            string DecryptingTenantShortName)
        {
            public static Parameters Random()
            {
                return new Parameters(
                    SingletonRandom.Instance.NextString(),
                    SingletonRandom.Instance.NextString(),
                    SingletonRandom.Instance.NextBoolean(),
                    SingletonRandom.Instance.NextBoolean(),
                    SingletonRandom.Instance.NextString(),
                    SingletonRandom.Instance.NextString(),
                    SingletonRandom.Instance.NextString());
            }
        };

        public override Command Create(IHost host)
        {
            var command = new Command("download-monitor", "Monitor a folder for study download requests.");

            var inputFolder = command.AddOption("--input-folder", "-i", "./", "The input folder to monitor.");
            var outputFolder = command.AddOption("--output-folder", "-o", "./", "The output folder in which to download studies.");
            var generateCsv = command.AddOption("--generate-csv", "-csv", false, "Generate CSV files from binary files.");
            var keepBinary = command.AddOption("--keep-binary", "-bin", true, "Do not delete binary files which have been processed into CSV files (faster).");
            var postProcessor = command.AddOption("--post-processor", "-pp", string.Empty, "The post processor to run on each downloaded study. The path to the study will be passed as the first argument.");
            var postProcessorArguments = command.AddOption("--post-processor-arguments", "-ppa", string.Empty, "The arguments to pass to the post-processor. The string {0} will be replaced with the unquoted study path.");
            var decryptingTenant = command.AddOption("--decrypting-tenant-short-name", "-d", string.Empty, "If specified the job files will be re-encrypted using the specified decrypting tenant's key.");

            command.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
            {
                var parameters = new Parameters(
                    parseResult.GetValue(inputFolder),
                    parseResult.GetValue(outputFolder),
                    parseResult.GetValue(generateCsv),
                    parseResult.GetValue(keepBinary),
                    parseResult.GetValue(postProcessor),
                    parseResult.GetValue(postProcessorArguments),
                    parseResult.GetValue(decryptingTenant));
                return host.Services.GetRequiredService<IRunDownloader>().ExecuteAsync(parameters, cancellationToken);
            });

            return command;
        }
    }
}
