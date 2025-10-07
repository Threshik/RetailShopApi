using RetailShopApi.DTOs;

namespace RetailShopApi.Services.Interfaces
{
    public interface ICartService
    {
        Task<IEnumerable<CartItemDto>> GetCartItemsAsync();
        Task<bool> AddToCartAsync(int productId, int quantity);
        Task<bool> RemoveCartItemAsync(int id);
        Task<bool> ClearCartAsync();
        Task<bool> UpdateCartItemQuantityAsync(UpdateCartItemDto dto);
    }
}
