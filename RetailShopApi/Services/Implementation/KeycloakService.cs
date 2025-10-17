using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RetailShopApi.Services
{
    public class KeycloakService
    {
        private readonly HttpClient _httpClient;
        private readonly string _realm;
        private readonly string _serverUrl;
        private readonly string _clientId;
        private readonly string _clientSecret;

        public KeycloakService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();

            var keycloakConfig = configuration.GetSection("Keycloak");
            _realm = keycloakConfig["Realm"];
            _serverUrl = keycloakConfig["ServerUrl"];
            _clientId = keycloakConfig["ManagementClientId"];
            _clientSecret = keycloakConfig["ManagementClientSecret"];
        }


        // Get admin token from Keycloak
        private async Task<string> GetServiceAccountTokenAsync()
        {
            var data = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret)
            });

            var response = await _httpClient.PostAsync(
                $"{_serverUrl}/realms/{_realm}/protocol/openid-connect/token", data);

            response.EnsureSuccessStatusCode();

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("access_token").GetString();
        }

        //Create user and extract Keycloak ID
        public async Task<string?> CreateUserAsync(
     string username,
     string email,
     string firstName,
     string lastName,
     string password,
     string phoneNumber = "",
     string gender = "")
        {
            try
            {
                var token = await GetServiceAccountTokenAsync();
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                // ✅ Correct attribute and body structure
                var user = new
                {
                    username,
                    email,
                    firstName,
                    lastName,
                    enabled = true,
                    emailVerified = false,
                    attributes = new Dictionary<string, string[]>
            {
                { "phoneNumber", new[] { phoneNumber ?? "" } },
                { "gender", new[] { gender ?? "" } }
            },
                    credentials = new[]
                    {
                new { type = "password", value = password, temporary = false }
            }
                };

                var content = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json");
                var result = await _httpClient.PostAsync($"{_serverUrl}/admin/realms/{_realm}/users", content);

                Console.WriteLine($"Keycloak response status: {result.StatusCode}");

                // ✅ Accept both 201 and 204 as success
                if (result.StatusCode == System.Net.HttpStatusCode.Created ||
                    result.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    if (result.Headers.TryGetValues("Location", out var locationValues))
                    {
                        var locationUrl = locationValues.FirstOrDefault();
                        if (!string.IsNullOrEmpty(locationUrl))
                        {
                            var keycloakId = locationUrl.Split('/').Last();
                            Console.WriteLine($"✅ Keycloak user created successfully: {keycloakId}");
                            return keycloakId;
                        }
                    }

                    Console.WriteLine("✅ User created successfully (no Location header).");
                    return "Success";
                }

                // ❌ Log failure reason
                var error = await result.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Keycloak create user failed: {result.StatusCode} - {error}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Exception during Keycloak user creation: {ex.Message}");
                return null;
            }
        }


        public async Task<string?> CreateClientAsync(string clientId, string redirectUri)
        {
            try
            {
                var token = await GetServiceAccountTokenAsync();
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var clientData = new
                {
                    clientId = clientId,
                    enabled = true,
                    publicClient = true,
                    redirectUris = new[] { redirectUri },
                    protocol = "openid-connect",
                    standardFlowEnabled = true,
                    directAccessGrantsEnabled = false

                };

                var content = new StringContent(JsonSerializer.Serialize(clientData), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_serverUrl}/admin/realms/{_realm}/clients", content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Keycloak create client failed: {response.StatusCode} - {errorBody}");
                    return null;
                }
                if (response.Headers.TryGetValues("Location", out var locationValues))
                {
                    var locationUrl = locationValues.FirstOrDefault();
                    if (!string.IsNullOrEmpty(locationUrl))
                    {
                        var keycloakClientId = locationUrl.Split('/').Last();
                        Console.WriteLine($"Keycloak client created successfully: {keycloakClientId}");
                        return keycloakClientId;
                    }
                }
                Console.WriteLine("Client created");
                return clientId;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during Keycloak client creation: {ex.Message}");
                return null;
            }
        }
    }
}