using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RetailShopApi.Data;
using RetailShopApi.Models.DTOs;
using RetailShopApi.Models.Entity;
using RetailShopApi.Services.Interfaces;
using System.Text.Json;

namespace RetailShopApi.Services.Implementation
{
    public class OrderService : IOrderService
    {
        private readonly ProductDbContext _context;
        private readonly IDistributedCache _distributedCache;
        private const string OrderCacheKeyPrefix = "orders:";

        public OrderService(ProductDbContext context, IDistributedCache distributedCache)
        {
            _context = context;
            _distributedCache = distributedCache;
        }

        public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync(int customerId)
        {
            var cacheKey = $"{OrderCacheKeyPrefix}{customerId}";
            var cachedData = await _distributedCache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
                return JsonSerializer.Deserialize<List<OrderDto>>(cachedData)!;

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.CustomerId == customerId)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Name,
                        Price = oi.Product.Price,
                        Quantity = oi.Quantity
                    }).ToList()
                })
                .ToListAsync();

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };

            await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(orders), cacheOptions);
            return orders;
        }

        public async Task<bool> PlaceOrderAsync(int customerId)
        {
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.CustomerId == customerId)
                .ToListAsync();

            if (cartItems == null || !cartItems.Any())
                return false;

            var totalAmount = cartItems.Sum(item => item.Product.Price * item.Quantity);

            var newOrder = new Order
            {
                CustomerId = customerId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalAmount,
                OrderItems = new List<OrderItem>()
            };

            foreach (var cartItem in cartItems)
            {
                newOrder.OrderItems.Add(new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity
                });
            }

            _context.Orders.Add(newOrder);
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            await _distributedCache.RemoveAsync($"{OrderCacheKeyPrefix}{customerId}");
            await _distributedCache.RemoveAsync($"cart:items:{customerId}");

            return true;
        }
    }
}
