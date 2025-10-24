using FS.Keycloak.RestApiClient.Api;
using FS.Keycloak.RestApiClient.Authentication.ClientFactory;
using FS.Keycloak.RestApiClient.Authentication.Flow;
using FS.Keycloak.RestApiClient.ClientFactory;
using FS.Keycloak.RestApiClient.Model;

namespace RetailShopApi.Services
{
    public class KeycloakService
    {
        private readonly string? _realm;
        private readonly string? _serverUrl;
        private readonly string? _clientId;
        private readonly string? _clientSecret;

        public KeycloakService(IConfiguration configuration)
        {
            var keycloakConfig = configuration.GetSection("Keycloak");
            _realm = keycloakConfig["Realm"];
            _serverUrl = keycloakConfig["ServerUrl"];
            _clientId = keycloakConfig["ManagementClientId"];
            _clientSecret = keycloakConfig["ManagementClientSecret"];
        }

        // Create user and extract Keycloak ID
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
                // Set up authentication
                var credentials = new ClientCredentialsFlow
                {
                    KeycloakUrl = _serverUrl,
                    Realm = _realm,
                    ClientId = _clientId,
                    ClientSecret = _clientSecret
                };

                using var httpClient = AuthenticationHttpClientFactory.Create(credentials);
                using var usersApi = ApiClientFactory.Create<UsersApi>(httpClient);

                // Create user representation with custom attributes
                var userRepresentation = new UserRepresentation
                {
                    Username = username,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Enabled = true,
                    EmailVerified = false,
                    Attributes = new Dictionary<string, List<string>>
                    {
                        { "phoneNumber", new List<string> { phoneNumber } },
                        { "gender", new List<string> { gender } }
                    },
                    Credentials = new List<CredentialRepresentation>
                    {
                        new CredentialRepresentation
                        {
                            Type = "password",
                            Value = password,
                            Temporary = false
                        }
                    }
                };


                await usersApi.PostUsersAsync(_realm, userRepresentation);


                var users = await usersApi.GetUsersAsync(_realm, username: username, exact: true);
                var createdUser = users?.FirstOrDefault();

                if (createdUser?.Id != null)
                {
                    Console.WriteLine($"Keycloak user '{username}' created successfully with ID: {createdUser.Id}");
                    return createdUser.Id;
                }

                Console.WriteLine("Keycloak user created but could not retrieve user ID.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during Keycloak user creation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<string?> CreateClientAsync(string clientId, string redirectUri = null)
        {
            try
            {
                var credentials = new ClientCredentialsFlow
                {
                    KeycloakUrl = _serverUrl,
                    Realm = _realm,
                    ClientId = _clientId,
                    ClientSecret = _clientSecret
                };

                using var httpClient = AuthenticationHttpClientFactory.Create(credentials);
                using var clientsApi = ApiClientFactory.Create<ClientsApi>(httpClient);

                var clientRepresentation = new ClientRepresentation
                {
                    ClientId = clientId,
                    Enabled = true,
                    PublicClient = false,
                    StandardFlowEnabled = true,
                    DirectAccessGrantsEnabled = true,
                    RedirectUris = redirectUri != null ? new List<string> { redirectUri } : new List<string> { "*" }
                };

                await clientsApi.PostClientsAsync(_realm, clientRepresentation);

                Console.WriteLine($"Keycloak client '{clientId}' created successfully.");
                return "Client created successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during Keycloak client creation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }
    }
}