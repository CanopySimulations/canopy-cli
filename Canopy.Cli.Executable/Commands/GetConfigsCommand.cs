﻿using Canopy.Cli.Executable.Services;
using System.IO;
using System.CommandLine;
using System.CommandLine.Invocation;
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

        public override Command Create()
        {
            var command = new Command("get-configs", "Lists or downloads configs of the specified type.");

            command.AddOption(new Option<string>(
                new[] { "--config-type", "-t" },
                description: $"The config type to request (e.g. car).",
                getDefaultValue: () => string.Empty));

            command.AddOption(new Option<string>(
                new[] { "--user-id", "-uid" },
                description: $"Filter by user ID.",
                getDefaultValue: () => string.Empty));

            command.AddOption(new Option<string>(
                new[] { "--username", "-u" },
                description: $"Filter by username.",
                getDefaultValue: () => string.Empty));

            command.AddOption(new Option<DirectoryInfo?>(
                new[] { "--output-folder", "-o" },
                description: $"The output folder in which to save the files (optional).",
                getDefaultValue: () => null));

            command.AddOption(new Option<string>(
                new[] { "--sim-version", "-v" },
                description: $"Get config for specific schema version (optional).",
                getDefaultValue: () => string.Empty));

            command.AddOption(new Option<bool>(
                new[] { "--unwrap" },
                description: $"Unwrap the config (removes metadata such as sim version required for importing).",
                getDefaultValue: () => false));

            command.AddOption(new Option<bool>(
                new[] { "--format", "-f" },
                description: $"Format the config JSON.",
                getDefaultValue: () => false));

            command.AddOption(new Option<string>(
                new [] { "--decrypting-tenant-short-name", "-d" }, 
                getDefaultValue: () => string.Empty, 
                description: "If specified the job files will be re-encrypted using the specified decrypting tenant's key."));

            command.Handler = CommandHandler.Create(async (IHost host, Parameters parameters) =>
                await host.Services.GetRequiredService<IGetConfigs>().ExecuteAsync(
                    parameters with 
                    {
                        ConfigType = CommandUtilities.ValueOrPrompt(parameters.ConfigType, "Config Type: ", "Config type is required.", false),
                    }));

            return command;
        }
    }
}
