using RetailShopApi.Models.DTOs;

namespace RetailShopApi.Services.Interfaces
{
    public interface ICartService
    {
        Task<IEnumerable<CartItemDto>> GetCartItemsAsync(int customerId);
        Task<bool> AddToCartAsync(int customerId, int productId, int quantity);
        Task<bool> RemoveCartItemAsync(int customerId, int id);
        Task<bool> ClearCartAsync(int customerId);
        Task<bool> UpdateCartItemQuantityAsync(int customerId, UpdateCartItemDto dto);
    }
}
