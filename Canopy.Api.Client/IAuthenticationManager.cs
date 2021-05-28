namespace Canopy.Api.Client
{
    using System.Threading.Tasks;

    public interface IAuthenticationManager
    {
        void ClearAuthenticatedUser();
        Task<AuthenticatedUser> GetAuthenticatedUser();
        bool LoadAuthenticatedUser();
        void SetAuthenticationInformation(string username, string tenantName, string password);
    }
}