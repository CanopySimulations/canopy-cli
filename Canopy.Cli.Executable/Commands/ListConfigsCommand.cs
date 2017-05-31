﻿using System;
using System.Threading.Tasks;
using Canopy.Api.Client;

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
			foreach (var item in result.QueryResults.Documents)
			{
				Console.WriteLine(item.Name);
			}
		}
    }
}
