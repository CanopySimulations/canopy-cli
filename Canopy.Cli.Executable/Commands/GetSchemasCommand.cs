using Canopy.Api.Client;
using Canopy.Cli.Executable.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace Canopy.Cli.Executable.Commands
{
    public class GetSchemasCommand : CanopyCommandBase
    {
        public record Parameters(
            DirectoryInfo OutputFolder,
            string SimVersion,
            string TenantId);

        public override Command Create()
        {
            var command = new Command("get-schemas", "Downloads the specified config schemas.");

            command.AddOption(new Option<DirectoryInfo>(
                new[] { "--output-folder", "-o" },
                description: $"The output folder in which to save the files (defaults to the current directory).",
                getDefaultValue: () => new DirectoryInfo("./")));

            command.AddOption(new Option<string>(
                new[] { "--sim-version", "-v" },
                description: $"The desired sim version to use (defaults to the current version).",
                getDefaultValue: () => Constants.CurrentSimVersion));

            command.AddOption(new Option<string>(
                new[] { "--tenant-id", "-t" },
                description: $"The desired tenant ID (admin only)."));
     
            command.Handler = CommandHandler.Create((IHost host, Parameters parameters) =>
                host.Services.GetRequiredService<IGetSchemas>().ExecuteAsync(parameters));

            return command;
        }
    }
}
