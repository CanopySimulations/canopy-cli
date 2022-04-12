namespace Canopy.Api.Client
{
    public class CanopyTokenApiConfiguration : ICanopyTokenApiConfiguration
    {
        public IAuthenticationManager AuthenticationManager { get; }
        public IConnectionManager ConnectionManager { get; }

        public CanopyTokenApiConfiguration(
            ITokenAuthenticationManager authenticationManager,
            IConnectionManager connectionManager)
        {
            this.ConnectionManager = connectionManager;
            this.AuthenticationManager = authenticationManager;
        }
    }
}