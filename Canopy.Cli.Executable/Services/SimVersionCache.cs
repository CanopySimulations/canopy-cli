using System.Threading.Tasks;
using Canopy.Api.Client;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Services
{
    public class SimVersionCache : ISimVersionCache
    {
        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly ISimVersionClient simVersionClient;
        private readonly ILogger<SimVersionCache> logger;
        private readonly object syncLock = new object();

        private Task<string>? simVersionTask = null;

        public SimVersionCache(
            IEnsureAuthenticated ensureAuthenticated,
            ISimVersionClient simVersionClient,
            ILogger<SimVersionCache> logger)
        {
            this.ensureAuthenticated = ensureAuthenticated;
            this.simVersionClient = simVersionClient;
            this.logger = logger;
        }

        public async Task<string> Get()
        {
            var authenticatedUser = await this.ensureAuthenticated.ExecuteAsync();

            lock (this.syncLock)
            {
                if (this.simVersionTask == null)
                {
                    this.logger.LogInformation("Requesting tenant sim version.");
                    this.simVersionTask = this.simVersionClient.GetSimVersionAsync(authenticatedUser.TenantId);
                }
            }

            return await this.simVersionTask;
        }

        public Task<string> GetOrSet(string? requestedSimVersion)
        {
            if(requestedSimVersion == null)
            {
                return this.Get();
            }

            this.Set(requestedSimVersion);
            return Task.FromResult(requestedSimVersion);
        }

        public void Set(string simVersion)
        {
            lock (this.syncLock)
            {
                this.simVersionTask = Task.FromResult(simVersion);
            }
        }
    }
}