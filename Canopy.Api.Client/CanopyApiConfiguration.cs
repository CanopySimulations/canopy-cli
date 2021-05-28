using System;
namespace Canopy.Api.Client
{
    public record CanopyApiConfiguration(
        IAuthenticationManager AuthenticationManager,
        IConnectionManager ConnectionManager);
}
