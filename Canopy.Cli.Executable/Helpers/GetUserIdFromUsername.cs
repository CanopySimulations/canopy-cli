namespace Canopy.Cli.Executable.Helpers
{
    using Canopy.Api.Client;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class GetUserIdFromUsername
    {
        public static readonly GetUserIdFromUsername Instance = new GetUserIdFromUsername();

        public async Task<string> ExecuteAsync(CanopyApiConfiguration configuration, string tenantId, string username)
        {
            Console.WriteLine("Looking up username...");
            var tenancyClient = new TenancyClient(configuration);
            var tenantUsers = await tenancyClient.GetTenantUsersAsync(tenantId);
            var user = tenantUsers.Users.FirstOrDefault(v => v.Username == username);
            return user?.UserId;
        }
    }
}
