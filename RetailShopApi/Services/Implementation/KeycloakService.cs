using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace RetailShopApi.Services
{
    public class KeycloakService
    {
        private readonly HttpClient _httpClient;
        private readonly string _realm = "demo";
        private readonly string _serverUrl = "http://localhost:8080";
        private readonly string _adminUser = "admin";
        private readonly string _adminPass = "admin";

        public KeycloakService()
        {
            _httpClient = new HttpClient();
        }

        // Get admin token from Keycloak
        private async Task<string> GetAdminTokenAsync()
        {
            var data = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", "admin-cli"),
                new KeyValuePair<string, string>("username", _adminUser),
                new KeyValuePair<string, string>("password", _adminPass),
                new KeyValuePair<string, string>("grant_type", "password"),
            });

            var response = await _httpClient.PostAsync(
                $"{_serverUrl}/realms/master/protocol/openid-connect/token", data);

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
                var token = await GetAdminTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

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
    }
}
