using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Canopy.Cli.Executable.Commands;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Services
{
    public class GetSchemas : IGetSchemas
    {
        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly ISimVersionClient simVersionClient;
        private readonly ILogger<GetSchemas> logger;

        public GetSchemas(
            IEnsureAuthenticated ensureAuthenticated,
            ISimVersionClient simVersionClient,
            ILogger<GetSchemas> logger)
        {
            this.ensureAuthenticated = ensureAuthenticated;
            this.simVersionClient = simVersionClient;
            this.logger = logger;
        }

        public async Task ExecuteAsync(GetSchemasCommand.Parameters parameters)
        {
            var authenticatedUser = await this.ensureAuthenticated.ExecuteAsync();

            var outputFolder = Utilities.GetCreatedOutputFolder(parameters.OutputFolder);

            var tenantId = parameters.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                tenantId = authenticatedUser.TenantId;
            }

            var simVersion = parameters.SimVersion;

            this.logger.LogInformation("Requesting schemas...");
            var result = await this.simVersionClient.GetDocumentsAsync(simVersion, tenantId);

            this.logger.LogInformation("Saving schemas...");
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
        }
    }
}