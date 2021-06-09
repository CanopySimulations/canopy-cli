using System;
using Canopy.Cli.Shared;

namespace Canopy.Api.Client
{
	public record AuthenticatedUser(
		string AccessToken,
		DateTime AccessTokenExpiry,
		string RefreshToken,
		string UserId,
		string TenantId)
	{
		public static AuthenticatedUser Random()
		{
			return new AuthenticatedUser(
				Guid.NewGuid().ToString(),
				DateTime.UtcNow.AddMinutes(SingletonRandom.Instance.NextDouble() * 30),
				Guid.NewGuid().ToString(),
				Guid.NewGuid().ToString(),
				Guid.NewGuid().ToString());
		}
	}
}
