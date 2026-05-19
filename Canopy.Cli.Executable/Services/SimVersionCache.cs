using System.Threading.Tasks;
using System.Threading;
using Canopy.Api.Client;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Services
{
    public class SimVersionCache : ISimVersionCache
    {
        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly ISimVersionClient simVersionClient;
        private readonly ILogger<SimVersionCache> logger;
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private Task<string>? simVersionTask;

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
            if (this.simVersionTask != null)
                return await this.simVersionTask;

            await this.semaphore.WaitAsync();
            try
            {
                if (this.simVersionTask == null)
                {
                    var authenticatedUser = await this.ensureAuthenticated.ExecuteAsync();
                    this.logger.LogInformation("Requesting tenant sim version.");
                    this.simVersionTask = this.simVersionClient.GetSimVersionAsync(authenticatedUser.TenantId);
                }
            }
            finally
            {
                this.semaphore.Release();
            }

            return await this.simVersionTask;
        }

        public Task<string> GetOrSet(string? requestedSimVersion)
        {
            if (string.IsNullOrWhiteSpace(requestedSimVersion))
                return this.Get();

            this.Set(requestedSimVersion);
            return Task.FromResult(requestedSimVersion);
        }

        public void Set(string simVersion)
        {
            this.semaphore.Wait();
            try
            {
                this.simVersionTask = Task.FromResult(simVersion);
            }
            finally
            {
                this.semaphore.Release();
            }
        }
    }
}
