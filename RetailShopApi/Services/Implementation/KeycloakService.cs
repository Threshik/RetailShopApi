using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Net.Http.Headers;

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
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                // Include custom attributes
                var user = new
                {
                    username,
                    email,
                    firstName,
                    lastName,
                    enabled = true,
                    attributes = new Dictionary<string, object?>
                    {
                        { "phoneNumber", phoneNumber },
                        { "gender", gender }
                    },
                    credentials = new[]
                    {
                        new { type = "password", value = password, temporary = false }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json");

                var result = await _httpClient.PostAsync($"{_serverUrl}/admin/realms/{_realm}/users", content);

                if (!result.IsSuccessStatusCode)
                {
                    var error = await result.Content.ReadAsStringAsync();
                    Console.WriteLine($"Keycloak create user failed: {error}");
                    return null;
                }

                // Extract user ID from the Location header
                if (result.Headers.TryGetValues("Location", out var locationValues))
                {
                    var locationUrl = locationValues.FirstOrDefault();
                    if (!string.IsNullOrEmpty(locationUrl))
                    {
                        var keycloakId = locationUrl.Split('/').Last();
                        Console.WriteLine($"Keycloak user created successfully: {keycloakId}");
                        return keycloakId;
                    }
                }

                Console.WriteLine("Keycloak user created but could not extract ID from Location header.");
                return null;
            } 
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during Keycloak user creation: {ex.Message}");
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
