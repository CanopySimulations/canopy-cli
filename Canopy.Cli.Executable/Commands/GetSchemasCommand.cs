using Canopy.Api.Client;
using Canopy.Cli.Executable.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.IO;
using System.Threading;

namespace Canopy.Cli.Executable.Commands
{
    public class GetSchemasCommand : CanopyCommandBase
    {
        public record Parameters(
            DirectoryInfo OutputFolder,
            string SimVersion,
            string TenantId);

        public override Command Create(IHost host)
        {
            var command = new Command("get-schemas", "Downloads the specified config schemas.");

            var outputFolder = command.AddOption("--output-folder", "-o", new DirectoryInfo("./"), "The output folder in which to save the files (defaults to the current directory).");
            var simVersion = command.AddOption("--sim-version", "-v", Constants.CurrentSimVersion, "The desired sim version to use (defaults to the current version).");
            var tenantId = command.AddOption("--tenant-id", "-t", string.Empty, "The desired tenant ID (admin only).");

            command.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
            {
                var parameters = new Parameters(
                    parseResult.GetValue(outputFolder),
                    parseResult.GetValue(simVersion),
                    parseResult.GetValue(tenantId));
                return host.Services.GetRequiredService<IGetSchemas>().ExecuteAsync(parameters, cancellationToken);
            });

            return command;
        }
    }
}
