﻿using System;
using System.Threading.Tasks;
using Canopy.Api.Client;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;

namespace Canopy.Cli.Executable.Commands
{
    public class ListConfigsCommand : CanopyCommandBase
    {
        private readonly CommandOption configTypeOption;

        public ListConfigsCommand()
        {
			this.Name = "list-configs";
			this.Description = "Lists configs.";

            this.configTypeOption = this.Option(
                "-t | --config-type",
                $"The config type to request (e.g. car).",
                CommandOptionType.SingleValue);
        }

        protected override async Task ExecuteAsync()
        {
            var configType = this.configTypeOption.ValueOrPrompt("Config Type: ", "Config type is required.");

			Console.WriteLine("Requesting configs...");
			var configClient = new ConfigClient(this.configuration);
			var result = await configClient.GetConfigsAsync(this.authenticatedUser.TenantId, configType, null, null);

            Utilities.WriteTable(
                new[] { "Name", "Id", "UserId" },
                result.QueryResults.Documents.Select(v => new string[] { v.Name, v.DocumentId, v.UserId }));
		}
    }
}
