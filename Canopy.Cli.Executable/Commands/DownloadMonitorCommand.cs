using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
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
            string PostProcessorArguments)
        {
            public static Parameters Random()
            {
                return new Parameters(
                    SingletonRandom.Instance.NextString(),
                    SingletonRandom.Instance.NextString(),
                    SingletonRandom.Instance.NextBoolean(),
                    SingletonRandom.Instance.NextBoolean(),
                    SingletonRandom.Instance.NextString(),
                    SingletonRandom.Instance.NextString());
            }
        };

        public override Command Create()
        {
            var command = new Command("download-monitor", "Monitor a folder for study download requests.");

            command.AddOption(new Option<string>(
                new [] { "--input-folder", "-i" }, 
                getDefaultValue: () => "./", 
                description: "The input folder to monitor."));

            command.AddOption(new Option<string>(
                new [] { "--output-folder", "-o" }, 
                getDefaultValue: () => "./", 
                description: "The output folder in which to download studies."));

            command.AddOption(new Option<bool>(
                new [] { "--generate-csv", "-csv" }, 
                getDefaultValue: () => false, 
                description: "Generate CSV files from binary files."));

            command.AddOption(new Option<bool>(
                new [] { "--keep-binary", "-bin" }, 
                getDefaultValue: () => true, 
                description: "Do not delete binary files which have been processed into CSV files (faster)."));

            command.AddOption(new Option<string>(
                new [] { "--post-processor", "-pp" }, 
                getDefaultValue: () => string.Empty, 
                description: "The post processor to run on each downloaded study. The path to the study will be passed as the first argument."));

            command.AddOption(new Option<string>(
                new [] { "--post-processor-arguments", "-ppa" }, 
                getDefaultValue: () => string.Empty, 
                description: "The arguments to pass to the post-processor. Use the string '{0}' where the path to the study should be, surrounding it in quotes if necessary."));

            command.Handler = CommandHandler.Create((IHost host, Parameters parameters) =>
                host.Services.GetRequiredService<IRunDownloader>().ExecuteAsync(parameters));

            return command;
        }
    }
}
