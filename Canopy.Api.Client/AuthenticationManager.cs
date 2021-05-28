﻿namespace Canopy.Api.Client
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Canopy.Api.Client;
    using Microsoft.Extensions.Logging;

    public class AuthenticationManager : IAuthenticationManager
    {
        private readonly string saveFolder;
        private readonly string saveAuthenticatedUserFile;
        private readonly IConnectionManager connectionManager;

        private string username;
        private string tenantName;
        private string password;

        private AuthenticatedUser authenticatedUser;
        private readonly ILogger<AuthenticationManager> logger;

        public AuthenticationManager(
            IConnectionManager connectionManager,
            ILogger<AuthenticationManager> logger)
        {
            this.logger = logger;
            this.connectionManager = connectionManager;
            this.saveFolder = PlatformUtilities.AppDataFolder();
            this.saveAuthenticatedUserFile = Path.Combine(this.saveFolder, "authenticated-user.json");
        }

        public void SetAuthenticationInformation(string username, string tenantName, string password)
        {
            this.username = username;
            this.tenantName = tenantName;
            this.password = password;
        }

        public void ClearAuthenticatedUser()
        {
            this.username = null;
            this.tenantName = null;
            this.password = null;
            this.authenticatedUser = null;
            this.DeleteAuthenticatedUser();
        }

        public bool LoadAuthenticatedUser()
        {
            if (File.Exists(this.saveAuthenticatedUserFile))
            {
                var json = File.ReadAllText(this.saveAuthenticatedUserFile);
                this.authenticatedUser = JsonConvert.DeserializeObject<AuthenticatedUser>(json);

                // Uncomment this to force getting a new access token on startup.
                /*
                this.authenticatedUser = new AuthenticatedUser(
                    this.authenticatedUser.AccessToken,
                    DateTime.MinValue,
                    this.authenticatedUser.RefreshToken,
                    this.authenticatedUser.UserId,
                    this.authenticatedUser.TenantId);
                */

                return true;
            }

            return false;
        }

        public async Task<AuthenticatedUser> GetAuthenticatedUser()
        {
            if (this.authenticatedUser == null)
            {
                if (string.IsNullOrWhiteSpace(this.username)
                   || string.IsNullOrWhiteSpace(this.tenantName)
                   || string.IsNullOrWhiteSpace(this.password))
                {
                    return null;
                }

                await this.Authenticate();
            }
            else if (DateTime.UtcNow > this.authenticatedUser.AccessTokenExpiry)
            {
                await this.RefreshAccessToken();
            }

            return this.authenticatedUser;
        }

        private void SaveAuthenticatedUser()
        {
            var json = JsonConvert.SerializeObject(this.authenticatedUser);
            File.WriteAllText(this.saveAuthenticatedUserFile, json);
        }

        private void DeleteAuthenticatedUser()
        {
            if (File.Exists(this.saveAuthenticatedUserFile))
            {
                File.Delete(this.saveAuthenticatedUserFile);
            }
        }

        private async Task Authenticate()
        {
            this.logger.LogInformation("Authenticating... ");
            try
            {
                var now = DateTime.UtcNow;
                var client = new HttpClient();
                var request = new HttpRequestMessage();
                var connection = this.connectionManager.Connection;
                request.Method = new HttpMethod("POST");
                request.RequestUri = new Uri(connection.Endpoint + "/token");
                request.Content = new StringContent(
                    $"grant_type=password&username={this.username}&tenant={this.tenantName}&password={this.password}" +
                    $"&client_id={connection.ClientId}&client_secret={connection.ClientSecret}",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded");

                var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                await ProcessTokenResponse(response, now);

                this.logger.LogInformation("Authenticated.");
            }
            catch (Exception)
            {
                this.logger.LogError("Failed to authenticate.");
                throw;
            }
        }

        private async Task RefreshAccessToken()
        {
            this.logger.LogInformation("Refreshing access token... ");
            try
            {
                var now = DateTime.UtcNow;
                var client = new HttpClient();
                var request = new HttpRequestMessage();
                var connection = this.connectionManager.Connection;
                request.Method = new HttpMethod("POST");
                request.RequestUri = new Uri(connection.Endpoint + "/token");
                request.Content = new StringContent(
                    $"grant_type=refresh_token&refresh_token={this.authenticatedUser.RefreshToken}&tenant={this.authenticatedUser.TenantId}" +
                    $"&client_id={connection.ClientId}&client_secret={connection.ClientSecret}",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded");

                var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                await ProcessTokenResponse(response, now);
                this.logger.LogInformation("Refreshed access token.");
            }
            catch (Exception)
            {
                // This is a sub-optimal way of handling an expired refresh token.
                // The application will quit and the user will be forced to log in again next time.
                this.logger.LogError("Failed to refresh access token. Re-authentication required.");
                this.DeleteAuthenticatedUser();
                throw;
            }
        }

        private async Task ProcessTokenResponse(HttpResponseMessage response, DateTime now)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var responseMessage = await response.Content.ReadAsStringAsync();
                throw new Exception($"Response {response.StatusCode}: {responseMessage}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JObject.Parse(responseString);

            var accessToken = responseJson["access_token"].Value<string>();
            var refreshToken = responseJson["refresh_token"].Value<string>();
            var expiresIn = responseJson["expires_in"].Value<int>();
            var userId = responseJson["user_id"].Value<string>();
            var tenantId = responseJson["tenant_id"].Value<string>();

            this.authenticatedUser = new AuthenticatedUser(accessToken, now.AddSeconds(expiresIn), refreshToken, userId, tenantId);
            this.SaveAuthenticatedUser();
        }
    }
}