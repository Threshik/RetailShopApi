using RetailShopApi.Models.DTOs;
using RetailShopApi.Services.Interfaces;

namespace RetailShopApi.Services.Implementation
{
    public class ClientService : IClientService
    {
        private readonly KeycloakService _keycloakService;

        public ClientService(KeycloakService keycloakService)
        {
            _keycloakService = keycloakService;
        }

        public async Task<bool> CreateClientAsync(ClientRequestDto request)
        {
            var result = await _keycloakService.CreateClientAsync(request.ClientId, request.RedirectUri);
            return result != null;
        }
    }
}
