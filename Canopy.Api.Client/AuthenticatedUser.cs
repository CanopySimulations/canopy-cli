using System;
namespace Canopy.Api.Client
{
	public class AuthenticatedUser
	{
		public AuthenticatedUser(string accessToken, DateTime accessTokenExpiry, string refreshToken, string userId, string tenantId)
		{
			AccessToken = accessToken;
			AccessTokenExpiry = accessTokenExpiry;
			RefreshToken = refreshToken;
			UserId = userId;
			TenantId = tenantId;
		}

		public string AccessToken { get; private set; }
		public DateTime AccessTokenExpiry { get; private set; }
		public string RefreshToken { get; private set; }

		public string UserId { get; private set; }
		public string TenantId { get; private set; }
	}
}
