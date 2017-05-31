﻿using System;
using System.Threading.Tasks;
using Canopy.Api.Client;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Canopy.Cli.Executable.Commands
{
    public class ListConfigsCommand : CanopyCommandBase
    {
        public ListConfigsCommand()
        {
			this.Name = "list";
			this.Description = "Lists configs.";
		}

        protected override async Task ExecuteAsync()
        {
			Console.WriteLine("Requesting configs...");
			var configClient = new ConfigClient(this.configuration);
			var result = await configClient.GetConfigsAsyncAsync(this.authenticatedUser.TenantId, "car", null, null);

            Utilities.WriteTable(
                new[] { "Name", "Id", "UserId" },
                result.QueryResults.Documents.Select(v => new string[] { v.Name, v.DocumentId, v.UserId }));
		}

    }
}
