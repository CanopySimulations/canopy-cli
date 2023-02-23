using System;
using System.Threading;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Canopy.Cli.Shared;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class ReEncryptFileTests
    {
        private readonly IEncryptionClient encryptionClient;
        private readonly IEnsureAuthenticated ensureAuthenticated;

        private readonly ReEncryptFile target;

        public ReEncryptFileTests()
        {
            this.encryptionClient = Substitute.For<IEncryptionClient>();
            this.ensureAuthenticated = Substitute.For<IEnsureAuthenticated>();

            this.target = new ReEncryptFile(
                this.encryptionClient,
                this.ensureAuthenticated);
        }

        [Fact]
        public async Task WhenDecryptingTenantShortNameNotSpecifiedItShouldThrow()
        {
            var contents = "{ a: 1 }";
            var decryptingTenantShortName = string.Empty;
            var simVersion = StudyDownloadMetadata.Random().SimVersion;
            var cancellationToken = new CancellationTokenSource().Token;

            await Assert.ThrowsAsync<InvalidOperationException>(() => this.target.ExecuteAsync(contents, decryptingTenantShortName, simVersion, cancellationToken));

            await this.ensureAuthenticated.DidNotReceive().ExecuteAsync();
        }

        [Fact]
        public async Task ItShouldReturnReEncryptedData()
        {
            var contents = "{ a: 1 }";
            var decryptingTenantShortName = SingletonRandom.Instance.NextString();
            var simVersion = StudyDownloadMetadata.Random().SimVersion;
            var cancellationToken = new CancellationTokenSource().Token;

            var authenticatedUser = AuthenticatedUser.Random();

            this.ensureAuthenticated.ExecuteAsync().Returns(Task.FromResult(authenticatedUser));

            var reEncryptedData = JToken.Parse("{ a: 2 }");
            this.encryptionClient.ReEncryptAsync(
                authenticatedUser.TenantId,
                Arg.Is<DataToReEncrypt>(
                    v => v.SimVersion == simVersion 
                    && v.DecryptingTenantShortName == decryptingTenantShortName
                    && v.Data.ToString() == JObject.Parse(contents).ToString()),
                cancellationToken)
                .Returns(Task.FromResult(new GetReEncryptedDataQueryResult
                {
                    Data = reEncryptedData,
                }));

            var result = await this.target.ExecuteAsync(contents, decryptingTenantShortName, simVersion, cancellationToken);

            Assert.Equal(
                reEncryptedData.ToString(Newtonsoft.Json.Formatting.Indented),
                result);
        }
    }
}