﻿using System;
using System.Threading.Tasks;
using Canopy.Api.Client;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;
using Canopy.Cli.Executable.Helpers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;

namespace Canopy.Cli.Executable.Commands
{
    public class GetConfigsCommand : CanopyCommandBase
    {
        private readonly CommandOption configTypeOption;
        private readonly CommandOption userIdOption;
        private readonly CommandOption usernameOption;
        private readonly CommandOption outputFolderOption;
        private readonly CommandOption simVersionOption;
        private readonly CommandOption unwrapOption;
        private readonly CommandOption formatOption;

        public GetConfigsCommand()
        {
			this.Name = "get-configs";
			this.Description = "Lists or downloads configs of the specified type.";

            this.configTypeOption = this.Option(
                "-t | --config-type",
                $"The config type to request (e.g. car).",
                CommandOptionType.SingleValue);

            this.userIdOption = this.Option(
                "-uid | --user-id",
                $"Filter by user ID.",
                CommandOptionType.SingleValue);

            this.usernameOption = this.Option(
                "-u | --username",
                $"Filter by username.",
                CommandOptionType.SingleValue);

            this.outputFolderOption = this.Option(
                "-o | --output-folder",
                $"The output folder in which to save the files (optional).",
                CommandOptionType.SingleValue);

            this.simVersionOption = this.Option(
                "-v | --sim-version",
                $"Get config for specific schema version (optional).",
                CommandOptionType.SingleValue);

            this.unwrapOption = this.Option(
                "--unwrap",
                $"Unwrap the config (removes metadata such as sim version required for importing).",
                CommandOptionType.NoValue);

            this.formatOption = this.Option(
                "-f | --format",
                $"Format the config JSON.",
                CommandOptionType.NoValue);
        }

        protected override async Task<int> ExecuteAsync()
        {
            var configType = this.configTypeOption.ValueOrPrompt("Config Type: ", "Config type is required.");

            string userId = null;
            if (this.usernameOption.HasValue())
            {
                var username = this.usernameOption.Value();
                userId = await GetUserIdFromUsername.Instance.ExecuteAsync(
                    this.configuration, this.authenticatedUser.TenantId, username);
            }
            else
            {
                userId = this.userIdOption.Value();
            }

            var filter = new JObject(
                new JProperty("continuationToken", null));

            if (!string.IsNullOrWhiteSpace(userId))
            {
                filter.Add(
                    new JProperty("filterUserId", userId));
            }

            Console.WriteLine("Requesting configs...");
			var configClient = new ConfigClient(this.configuration);
			GetConfigsQueryResult result = null;
            do
            {
                if (result != null)
                {
                    filter["continuationToken"] = result.QueryResults.ContinuationToken;
                }

                result = await configClient.GetConfigsAsync(
                    this.authenticatedUser.TenantId,
                    configType,
                    filter.ToString(Formatting.None),
                    null,
                    null);

                if (this.outputFolderOption.HasValue())
                {
                    var format = this.formatOption.HasValue();
                    var unwrap = this.unwrapOption.HasValue();
                    var outputFolder = Utilities.GetCreatedOutputFolder(this.outputFolderOption);
                    var simVersion = this.simVersionOption.Value();

                    var sizes = new List<int>();

                    foreach (var configMetadata in result.QueryResults.Documents)
                    {
                        Console.WriteLine($"Downloading {configMetadata.Name}...");

                        var config = await configClient.GetConfigAsync(
                            configMetadata.TenantId,
                            configMetadata.UserId,
                            configMetadata.DocumentId,
                            null,
                            simVersion,
                            null);

                        var content = JObject.FromObject(config.Config.Data);
                        if (!unwrap)
                        {
                            content = new JObject(
                                new JProperty("simVersion", config.ConvertedSimVersion),
                                new JProperty("config", content));
                        }

                        var formatting = format ? Formatting.Indented : Formatting.None;

                        var contentString = content.ToString(formatting);

                        File.WriteAllText(
                            Path.Combine(outputFolder, FileNameUtilities.Sanitize(config.Config.Name) + ".json"),
                            contentString);

                        sizes.Add(contentString.Length);
                    }

                    Utilities.WriteTable(
                        new[] { "Name", "Id", "UserId", "Size" },
                        result.QueryResults.Documents
                            .Zip(sizes, (d, s) => new { d, s }).Select(v => new string[] { v.d.Name, v.d.DocumentId, v.d.UserId, v.s.ToString() }));
                }
                else
                {
                    Utilities.WriteTable(
                        new[] { "Name", "Id", "UserId" },
                        result.QueryResults.Documents.Select(v => new string[] { v.Name, v.DocumentId, v.UserId }));
                }
            }
            while(result.QueryResults.HasMoreResults == true);

            return 0;
        }
    }
}
