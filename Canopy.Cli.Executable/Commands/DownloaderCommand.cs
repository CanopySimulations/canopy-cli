using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Canopy.Cli.Executable.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Canopy.Cli.Executable.Commands
{
    public class DownloaderCommand : CanopyCommandBase
    {
        public record Parameters(
            DirectoryInfo InputFolder,
            DirectoryInfo OutputFolder,
            bool GenerateCsv,
            bool KeepBinary);

        public override Command Create()
        {
            var command = new Command("downloader", "Downloads the specified study or study job.");

            command.AddOption(new Option<DirectoryInfo>(
                new [] { "--input-folder", "-i" }, 
                getDefaultValue: () => new DirectoryInfo("./"), 
                description: "The input folder to monitor (defaults to the user's download directory)."));

            command.AddOption(new Option<DirectoryInfo>(
                new [] { "--output-folder", "-o" }, 
                getDefaultValue: () => new DirectoryInfo("./"), 
                description: "The output folder in which to save the files (defaults to the current directory)."));

            command.AddOption(new Option<bool>(
                new [] { "--generate-csv", "-csv" }, 
                getDefaultValue: () => false, 
                description: "Generate CSV files from binary files."));

            command.AddOption(new Option<bool>(
                new [] { "--keep-binary", "-bin" }, 
                getDefaultValue: () => true, 
                description: "Do not delete binary files which have been processed into CSV files (faster)."));

            command.Handler = CommandHandler.Create((IHost host, Parameters parameters) =>
                host.Services.GetRequiredService<IRunDownloader>().ExecuteAsync(parameters));

            return command;
        }
    }
}