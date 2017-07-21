using Canopy.Api.Client;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Commands
{
    class GetSchemasCommand : CanopyCommandBase
    {
        private readonly CommandOption outputFolderOption;
        private readonly CommandOption simVersionOption;
        private readonly CommandOption tenantIdOption;

        public GetSchemasCommand()
        {
            this.Name = "get-schemas";
            this.Description = "Downloads the specified config schemas.";

            this.outputFolderOption = this.Option(
                "-o | --output-folder",
                $"The output folder in which to save the files (defaults to the current directory).",
                CommandOptionType.SingleValue);

            this.simVersionOption = this.Option(
                "-v | --sim-version",
                $"The desired sim version to use (defaults to the current version).",
                CommandOptionType.SingleValue);

            this.tenantIdOption = this.Option(
                "-t | --tenant-id",
                $"The desired tenant ID (admin only).",
                CommandOptionType.SingleValue);
        }

        protected override async Task<int> ExecuteAsync()
        {
            var outputFolder = Utilities.GetCreatedOutputFolder(this.outputFolderOption);

            var tenantId = this.tenantIdOption.Value();
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                tenantId = this.authenticatedUser.TenantId;
            }

            var simVersion = this.simVersionOption.Value();
            if (string.IsNullOrWhiteSpace(simVersion))
            {
                simVersion = Constants.CurrentSimVersion;
            }

            Console.WriteLine("Requesting schemas...");
            var client = new SimVersionClient(this.configuration);
            var result = await client.GetDocumentsAsync(simVersion, tenantId);

            Console.WriteLine("Saving schemas...");
            var writtenFiles = new List<TextDocumentOptionalContent>();
            foreach (var document in result.Documents)
            {
                if (string.IsNullOrWhiteSpace(document.Content))
                {
                    continue;
                }

                if (!document.Name.EndsWith(".schema.json"))
                {
                    continue;
                }

                File.WriteAllText(Path.Combine(outputFolder, document.Name), document.Content);
                writtenFiles.Add(document);
            }

            Utilities.WriteTable(
                new[] { "Name", "Size" },
                writtenFiles.Select(v => new string[] { v.Name, v.Content.Length.ToString() }));

            return 0;
        }
    }
}
