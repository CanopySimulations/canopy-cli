namespace Canopy.Cli.Executable.Commands
{
    using System.CommandLine;
    using Canopy.Cli.Executable.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System.IO;
    using System.Threading;

    public class ReEncryptJsonFileCommand : CanopyCommandBase
    {
        public record Parameters(
            FileInfo Target,
            string SimVersion,
            string DecryptingTenantShortName);

        public override Command Create(IHost host)
        {
            var command = new Command("re-encrypt-json-file", "Re-encrypts the specified JSON file with the specified tenant and sim version key.");

            var target = command.AddRequiredOption<FileInfo?>("--target", "-t", "The file to process.");
            var decryptingTenant = command.AddOption("--decrypting-tenant-short-name", "-d", string.Empty, "If specified the job files will be re-encrypted using the specified decrypting tenant's key.");
            var simVersion = command.AddOption("--sim-version", "-v", string.Empty, "Get config for specific schema version (optional).");

            command.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
            {
                var parameters = new Parameters(
                    parseResult.GetValue(target)!,
                    parseResult.GetValue(simVersion),
                    parseResult.GetValue(decryptingTenant));
                return host.Services.GetRequiredService<IReEncryptJsonFile>().ExecuteAsync(
                    parameters with
                    {
                        DecryptingTenantShortName = CommandUtilities.ValueOrPrompt(parameters.DecryptingTenantShortName, "Decrypting Tenant Short Name: ", "The decrypting tenant short name is required.", false),
                    }, cancellationToken);
            });

            return command;
        }
    }
}