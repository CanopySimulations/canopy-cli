namespace Canopy.Api.Client
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class CanopyApiClient
    {
        private readonly ICanopyApiConfiguration configuration;

        public CanopyApiClient(ICanopyApiConfiguration configuration)
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

            var authenticatedUser = await this.configuration.AuthenticationManager.GetAuthenticatedUser();

            if (authenticatedUser != null)
            {
                result.Headers.Add("Authorization", "Bearer " + authenticatedUser.AccessToken);
            }

            return result;
        }

        protected Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, System.Text.StringBuilder urlBuilder, CancellationToken cancellationToken)
        {
            // ASP.NET Web API doesn't like having trailing ampersands in URLs.
            if (urlBuilder[urlBuilder.Length - 1] == '&')
            {
                urlBuilder.Remove(urlBuilder.Length - 1, 1);
            }

            var connection = this.configuration.ConnectionManager.Connection;
            urlBuilder.Insert(0, '/').Insert(0, connection.Endpoint);

            return Task.CompletedTask;
        }

        protected Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, string url, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected Task ProcessResponseAsync(HttpClient client, HttpResponseMessage response, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
