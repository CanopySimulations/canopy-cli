using System;
using System.Threading;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Newtonsoft.Json.Linq;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class ReEncryptFile : IReEncryptFile
    {
        private readonly IEncryptionClient encryptionClient;
        private readonly IEnsureAuthenticated ensureAuthenticated;

        public ReEncryptFile(IEncryptionClient encryptionClient, IEnsureAuthenticated ensureAuthenticated)
        {
            this.encryptionClient = encryptionClient;
            this.ensureAuthenticated = ensureAuthenticated;
        }

        public async Task<string> ExecuteAsync(
            string contents,
            string decryptingTenantShortName,
            string simVersion,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(decryptingTenantShortName))
            {
                throw new InvalidOperationException("A decrypting tenant short name must be supplied to re-encrypt a file.");
            }

            var authenticatedUser = await this.ensureAuthenticated.ExecuteAsync();

            var result = await this.encryptionClient.ReEncryptAsync(
                authenticatedUser.TenantId,
                new DataToReEncrypt
                {
                    Data = JObject.Parse(contents),
                    SimVersion = simVersion,
                    DecryptingTenantShortName = decryptingTenantShortName,
                },
                cancellationToken);

            if (result.Data is not JToken jsonResult)
            {
                throw new InvalidOperationException("Result of re-encryption was not the expected JSON content.");
            }

            return jsonResult.ToString(Newtonsoft.Json.Formatting.Indented);
        }
    }
}