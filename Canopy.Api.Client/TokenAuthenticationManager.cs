using System.Threading.Tasks;

namespace Canopy.Api.Client
{
    public class TokenAuthenticationManager : ITokenAuthenticationManager
    {
        public void ClearAuthenticatedUser()
        {
            throw new System.NotImplementedException();
        }

        public Task<AuthenticatedUser> GetAuthenticatedUser()
        {
            return Task.FromResult<AuthenticatedUser>(null);
        }

        public bool LoadAuthenticatedUser()
        {
            throw new System.NotImplementedException();
        }

        public void SetAuthenticationInformation(string username, string tenantName, string password)
        {
            throw new System.NotImplementedException();
        }
    }
}