namespace Canopy.Api.Client
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class CanopyApiClient
    {
        private readonly CanopyApiConfiguration configuration;
        public CanopyApiClient(CanopyApiConfiguration configuration)
        {
            this.configuration = configuration;
        }

        protected Task<HttpClient> CreateHttpClientAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpClient());
        }

        protected async Task<HttpRequestMessage> CreateHttpRequestMessageAsync(CancellationToken cancellationToken)
        {
            var result = new HttpRequestMessage();

            var authenticatedUser = await CanopyAuthentication.Instance.GetAuthenticatedUser();
            result.Headers.Add("Authorization", "Bearer " + authenticatedUser.AccessToken);

            return result;
        }

        protected virtual void PrepareRequest(System.Net.Http.HttpClient client, System.Net.Http.HttpRequestMessage request, System.Text.StringBuilder urlBuilder)
        {
            // ASP.NET Web API doesn't like having trailing ampersands in URLs.
            if (urlBuilder[urlBuilder.Length] == '&')
            {
                urlBuilder.Remove(urlBuilder.Length - 1, 1);
            }

            urlBuilder.Insert(0, '/').Insert(0, this.configuration.BaseUrl);
        }
    }
}
