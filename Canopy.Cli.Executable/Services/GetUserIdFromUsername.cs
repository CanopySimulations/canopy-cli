namespace Canopy.Cli.Executable.Services
{
    using Canopy.Api.Client;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class GetUserIdFromUsername : IGetUserIdFromUsername
    {
        private readonly ILogger<GetUserIdFromUsername> logger;
        private readonly ITenancyClient tenancyClient;

        public GetUserIdFromUsername(
            ITenancyClient tenancyClient,
            ILogger<GetUserIdFromUsername> logger)
        {
            this.tenancyClient = tenancyClient;
            this.logger = logger;
        }

        public async Task<string?> ExecuteAsync(string tenantId, string username)
        {
            this.logger.LogInformation("Looking up username...");

            var tenantUsers = await this.tenancyClient.GetTenantUsersAsync(tenantId);
            var user = tenantUsers.Users.FirstOrDefault(v => v.Username == username);
            return user?.UserId;
        }
    }
}
