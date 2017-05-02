namespace Canopy.Cli.Executable
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

    public class CanopyAuthentication
    {
        public const string CanopyApiBaseUrl = "https://api.canopysimulations.com";
        public const string ClientId = "";
        public const string ClientSecret = "";

        public static readonly CanopyApiConfiguration Configuration = new CanopyApiConfiguration(CanopyApiBaseUrl);
        public static readonly CanopyAuthentication Instance = new CanopyAuthentication();

        private readonly string saveFolder;
        private readonly string saveFile;

        private string username;
        private string tenantName;
        private string password;

        private AuthenticatedUser authenticatedUser;

        public CanopyAuthentication()
        {
            this.saveFolder = PlatformUtilities.AppDataFolder();
            this.saveFile = Path.Combine(this.saveFolder, "authenticated-user.json");
        }

        public void SetAuthenticationInformation(string username, string tenantName, string password)
        {
            this.username = username;
            this.tenantName = tenantName;
            this.password = password;
        }

        public bool LoadAuthenticatedUser()
        {
            if (File.Exists(this.saveFile))
            {
                var json = File.ReadAllText(this.saveFile);
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

                Console.WriteLine("Loaded user from: " + this.saveFile);

                return true;
            }

            return false;
        }

        private void SaveAuthenticatedUser()
        {
            if (!Directory.Exists(this.saveFolder))
            {
                Directory.CreateDirectory(this.saveFolder);
            }

            var json = JsonConvert.SerializeObject(this.authenticatedUser);
            File.WriteAllText(this.saveFile, json);
        }

        private void DeleteAuthenticatedUser()
        {
            if (File.Exists(this.saveFile))
            {
                File.Delete(this.saveFile);
            }
        }

        public async Task<AuthenticatedUser> GetAuthenticatedUser()
        {
            if (this.authenticatedUser == null)
            {
                await this.Authenticate();
            }
            else if (DateTime.UtcNow > this.authenticatedUser.AccessTokenExpiry)
            {
                await this.RefreshAccessToken();
            }

            return this.authenticatedUser;
        }

        private async Task Authenticate()
        {
            Console.WriteLine("Authenticating...");
            var now = DateTime.UtcNow;
            var client = new HttpClient();
            var request = new HttpRequestMessage();
            request.Method = new HttpMethod("POST");
            request.RequestUri = new Uri(CanopyApiBaseUrl + "/token");
            request.Content = new StringContent(
                $"grant_type=password&username={this.username}&tenant={this.tenantName}&password={this.password}" +
                $"&client_id={ClientId}&client_secret={ClientSecret}",
                Encoding.UTF8,
                "application/x-www-form-urlencoded");

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            await ProcessTokenResponse(response, now);
        }

        private async Task RefreshAccessToken()
        {
            try
            {
                Console.WriteLine("Refreshing access token...");
                var now = DateTime.UtcNow;
                var client = new HttpClient();
                var request = new HttpRequestMessage();
                request.Method = new HttpMethod("POST");
                request.RequestUri = new Uri(CanopyApiBaseUrl + "/token");
                request.Content = new StringContent(
                    $"grant_type=refresh_token&refresh_token={this.authenticatedUser.RefreshToken}&tenant={this.authenticatedUser.TenantId}" +
                    $"&client_id={ClientId}&client_secret={ClientSecret}",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded");

                var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                await ProcessTokenResponse(response, now);
            }
            catch (Exception)
            {
                // This is a crappy way of handling an expired refresh token.
                // The application will quit and the user will be forced to log in again next time.
                Console.WriteLine("Failed to refresh access token.");
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