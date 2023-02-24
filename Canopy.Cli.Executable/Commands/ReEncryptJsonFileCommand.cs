namespace Canopy.Cli.Executable.Commands
{
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using Canopy.Cli.Executable.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System.IO;

    public class ReEncryptJsonFileCommand : CanopyCommandBase
    {
        public record Parameters(
            FileInfo Target,
            string SimVersion,
            string DecryptingTenantShortName);

        public override Command Create()
        {
            var command = new Command("re-encrypt-json-file", "Re-encrypts the specified JSON file with the specified tenant and sim version key.");

            command.AddOption(new Option<FileInfo?>(
                new[] { "--target", "-t" },
                description: $"The file to process.")
                {
                    IsRequired = true,
                });

            command.AddOption(new Option<string>(
                new [] { "--decrypting-tenant-short-name", "-d" },
                getDefaultValue: () => string.Empty, 
                description: "If specified the job files will be re-encrypted using the specified decrypting tenant's key."));

            command.AddOption(new Option<string>(
                new[] { "--sim-version", "-v" },
                getDefaultValue: () => string.Empty,
                description: $"Get config for specific schema version (optional)."));

            command.Handler = CommandHandler.Create((IHost host, Parameters parameters) =>
                host.Services.GetRequiredService<IReEncryptJsonFile>().ExecuteAsync(
                    parameters with
                    {
                        DecryptingTenantShortName = CommandUtilities.ValueOrPrompt(parameters.DecryptingTenantShortName, "Decrypting Tenant Short Name: ", "The decrypting tenant short name is required.", false),
                    }));

            return command;
        }
    }
}