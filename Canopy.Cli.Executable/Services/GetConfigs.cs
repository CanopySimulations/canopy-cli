using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Canopy.Cli.Executable.Commands;
using Canopy.Cli.Executable.Services.DownloadMonitoring;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Canopy.Cli.Executable.Services
{
    public class GetConfigs : IGetConfigs
    {
        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly ISimVersionCache simVersionCache;
        private readonly IGetUserIdFromUsername getUserIdFromUsername;
        private readonly IConfigClient configClient;
        private readonly IWriteFile writeFile;
        private readonly IGetCreatedOutputFolder getCreatedOutputFolder;
        private readonly IReEncryptFile reEncryptFile;
        private readonly ILogger<GetConfigs> logger;

        public GetConfigs(
            IEnsureAuthenticated ensureAuthenticated,
            ISimVersionCache simVersionCache,
            IGetUserIdFromUsername getUserIdFromUsername,
            IConfigClient configClient,
            IWriteFile writeFile,
            IGetCreatedOutputFolder getCreatedOutputFolder,
            IReEncryptFile reEncryptFile,
            ILogger<GetConfigs> logger)
        {
            this.ensureAuthenticated = ensureAuthenticated;
            this.simVersionCache = simVersionCache;
            this.getUserIdFromUsername = getUserIdFromUsername;
            this.configClient = configClient;
            this.writeFile = writeFile;
            this.getCreatedOutputFolder = getCreatedOutputFolder;
            this.reEncryptFile = reEncryptFile;
            this.logger = logger;
        }

        public async Task ExecuteAsync(
            GetConfigsCommand.Parameters parameters)
        {
            var configType = parameters.ConfigType;
            var username = parameters.Username;
            var userId = parameters.UserId;
            var outputFolder = parameters.OutputFolder;
            var format = parameters.Format;
            var unwrap = parameters.Unwrap;
            
            var simVersion = await this.simVersionCache.GetOrSet(parameters.SimVersion);

            var authenticatedUser = await this.ensureAuthenticated.ExecuteAsync();

            if (!string.IsNullOrWhiteSpace(username))
            {
                userId = await getUserIdFromUsername.ExecuteAsync(authenticatedUser.TenantId, username);
            }

            var filter = new JObject(
                new JProperty("continuationToken", null));

            if (!string.IsNullOrWhiteSpace(userId))
            {
                filter.Add(
                    new JProperty("filterUserId", userId));
            }

            this.logger.LogInformation("Requesting configs...");
            GetConfigsQueryResult? result = null;
            do
            {
                if (result != null)
                {
                    filter["continuationToken"] = result.QueryResults.ContinuationToken;
                }

                result = await this.configClient.GetConfigsAsync(
                    authenticatedUser.TenantId,
                    configType,
                    filter.ToString(Formatting.None),
                    null,
                    null);

                if (outputFolder != null)
                {
                    var outputFolderPath = this.getCreatedOutputFolder.Execute(outputFolder);

                    var sizes = new List<int>();

                    foreach (var configMetadata in result.QueryResults.Documents)
                    {
                        this.logger.LogInformation($"Downloading {configMetadata.Name}...");

                        var config = await this.configClient.GetConfigAsync(
                            configMetadata.TenantId,
                            configMetadata.DocumentId,
                            null,
                            simVersion,
                            null);

                        var content = JObject.FromObject(config.Config.Data);

                        if (!string.IsNullOrWhiteSpace(parameters.DecryptingTenantShortName))
                        {
                            var reEncryptedContent = await this.reEncryptFile.ExecuteAsync(
                                content.ToString(Formatting.None),
                                parameters.DecryptingTenantShortName,
                                simVersion,
                                CancellationToken.None);
                            content = JObject.Parse(reEncryptedContent);
                        }

                        if (!unwrap)
                        {
                            content = new JObject(
                                new JProperty("simVersion", config.ConvertedSimVersion),
                                new JProperty("config", content));
                        }

                        var formatting = format ? Formatting.Indented : Formatting.None;

                        var contentString = content.ToString(formatting);

                        await this.writeFile.ExecuteAsync(
                            Path.Combine(outputFolderPath, FileNameUtilities.Sanitize(config.Config.Name) + ".json"),
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
            while (result.QueryResults.HasMoreResults == true);
        }
    }
}