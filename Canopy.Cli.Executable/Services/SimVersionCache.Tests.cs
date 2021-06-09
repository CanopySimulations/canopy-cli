using System.Threading.Tasks;
using Canopy.Api.Client;
using Canopy.Cli.Shared;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Canopy.Cli.Executable.Services
{
    public class SimVersionCacheTests
    {
        protected static string TenantId = SingletonRandom.Instance.NextString();
        protected static string SimVersion = SingletonRandom.Instance.NextString();

        protected readonly IEnsureAuthenticated ensureAuthenticated;
        protected readonly ISimVersionClient simVersionClient;
        protected readonly ILogger<SimVersionCache> logger;

        protected readonly SimVersionCache target;

        public SimVersionCacheTests()
        {
            this.ensureAuthenticated = Substitute.For<IEnsureAuthenticated>();
            this.simVersionClient = Substitute.For<ISimVersionClient>();
            this.logger = Substitute.For<ILogger<SimVersionCache>>();

            this.target = new SimVersionCache(
                this.ensureAuthenticated,
                this.simVersionClient,
                this.logger);
        }

        public class Get : SimVersionCacheTests
        {
            [Fact]
            public async Task ItShouldOnlyRequestSimVersionForTenantOncePerInstance()
            {
                this.ensureAuthenticated.ExecuteAsync().Returns(this.GetAuthenticatedUserTask(TenantId));
                this.simVersionClient.GetSimVersionAsync(TenantId).Returns(Task.FromResult(SimVersion));

                var result1 = await this.target.Get();
                var result2 = await this.target.Get();

                Assert.Equal(result1, SimVersion);
                Assert.Equal(result1, result2);

                await this.simVersionClient.Received(1).GetSimVersionAsync(TenantId);
            }
        }

        public class Set : SimVersionCacheTests
        {
            [Fact]
            public async Task ItShouldSetCurrentSimVersionWithoutQueryingApi()
            {
                this.target.Set(SimVersion);

                var result1 = await this.target.Get();
                var result2 = await this.target.Get();

                Assert.Equal(result1, SimVersion);
                Assert.Equal(result1, result2);
          
                await this.ensureAuthenticated.Received(0).ExecuteAsync();
                await this.simVersionClient.ReceivedWithAnyArgs(0).GetSimVersionAsync(Arg.Any<string>());
            }
        }

        public class GetOrSet : SimVersionCacheTests
        {
            [Fact]
            public async Task WhenRequestedSimVersionIsNullItShouldGetCurrentSimVersion()
            {
                this.ensureAuthenticated.ExecuteAsync().Returns(this.GetAuthenticatedUserTask(TenantId));
                this.simVersionClient.GetSimVersionAsync(TenantId).Returns(Task.FromResult(SimVersion));
             
                var result1 = await this.target.GetOrSet(null);
                var result2 = await this.target.GetOrSet(null);
                var result3 = await this.target.Get();

                Assert.Equal(result1, SimVersion);
                Assert.Equal(result1, result2);
                Assert.Equal(result1, result3);
          
                await this.simVersionClient.Received(1).GetSimVersionAsync(TenantId);
            }

            [Fact]
            public async Task WhenRequestedSimVersionIsNotNullItShouldReturnRequestedVersionAndPopulateCache()
            {
                var result1 = await this.target.GetOrSet(SimVersion);
                var result2 = await this.target.Get();

                Assert.Equal(result1, SimVersion);
                Assert.Equal(result1, result2);
          
                await this.ensureAuthenticated.Received(0).ExecuteAsync();
                await this.simVersionClient.ReceivedWithAnyArgs(0).GetSimVersionAsync(Arg.Any<string>());
            }
        }

        protected Task<AuthenticatedUser> GetAuthenticatedUserTask(string tenantId)
        {
            return Task.FromResult(
                AuthenticatedUser.Random() with
                {
                    TenantId = tenantId
                });
        }
    }
}