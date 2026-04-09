﻿using Canopy.Cli.Executable.Services;
using System.IO;
using System.CommandLine;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Canopy.Cli.Executable.Commands
{
    public class GetConfigsCommand : CanopyCommandBase
    {
        public record Parameters(
            string ConfigType,
            string UserId,
            string Username,
            DirectoryInfo? OutputFolder,
            string SimVersion,
            bool Unwrap,
            bool Format,
            string DecryptingTenantShortName);

        public override Command Create(IHost host)
        {
            var command = new Command("get-configs", "Lists or downloads configs of the specified type.");

            var configType = command.AddOption("--config-type", "-t", string.Empty, "The config type to request (e.g. car).");
            var userId = command.AddOption("--user-id", "-uid", string.Empty, "Filter by user ID.");
            var username = command.AddOption("--username", "-u", string.Empty, "Filter by username.");
            var outputFolder = command.AddOption<DirectoryInfo?>("--output-folder", "-o", null, "The output folder in which to save the files (optional).");
            var simVersion = command.AddOption("--sim-version", "-v", string.Empty, "Get config for specific schema version (optional).");
            var unwrap = command.AddOption("--unwrap", false, "Unwrap the config (removes metadata such as sim version required for importing).");
            var format = command.AddOption("--format", "-f", false, "Format the config JSON.");
            var decryptingTenant = command.AddOption("--decrypting-tenant-short-name", "-d", string.Empty, "If specified the job files will be re-encrypted using the specified decrypting tenant's key.");

            command.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
            {
                var parameters = new Parameters(
                    parseResult.GetValue(configType),
                    parseResult.GetValue(userId),
                    parseResult.GetValue(username),
                    parseResult.GetValue(outputFolder),
                    parseResult.GetValue(simVersion),
                    parseResult.GetValue(unwrap),
                    parseResult.GetValue(format),
                    parseResult.GetValue(decryptingTenant));
                return host.Services.GetRequiredService<IGetConfigs>().ExecuteAsync(
                    parameters with
                    {
                        ConfigType = CommandUtilities.ValueOrPrompt(parameters.ConfigType, "Config Type: ", "Config type is required.", false),
                    },
                    cancellationToken);
            });

            return command;
        }
    }
}
