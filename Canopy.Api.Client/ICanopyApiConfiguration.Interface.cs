namespace Canopy.Api.Client
{
    public interface ICanopyApiConfiguration
    {
        IAuthenticationManager AuthenticationManager { get; }
        IConnectionManager ConnectionManager { get; }
    }
}
