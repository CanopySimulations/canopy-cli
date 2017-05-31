using System;
namespace Canopy.Api.Client
{
	public class ConnectionInformation
	{
		public ConnectionInformation(string endpoint, string clientId, string clientSecret)
		{
			this.Endpoint = endpoint;
			this.ClientId = clientId;
			this.ClientSecret = clientSecret;
		}

		public string Endpoint { get; private set; }
		public string ClientId { get; private set; }
		public string ClientSecret { get; private set; }
	}
}
