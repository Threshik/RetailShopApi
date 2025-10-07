using RetailShopApi.Models.DTOs;

namespace RetailShopApi.Services.Interfaces
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
        Task<bool> PlaceOrderAsync();
    }
}
