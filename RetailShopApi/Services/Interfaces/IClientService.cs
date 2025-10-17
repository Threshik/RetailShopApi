using RetailShopApi.Models.DTOs;

namespace RetailShopApi.Services.Interfaces
{
    public interface IClientService
    {
        Task<bool> CreateClientAsync(ClientRequestDto request);
    }
}
