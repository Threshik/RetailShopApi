using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RetailShopApi.Data;
using RetailShopApi.Models.DTOs;
using RetailShopApi.Models.Entity;
using RetailShopApi.Services.Interfaces;
using System.Text.Json;

namespace RetailShopApi.Services.Implementation
{
    public class CartService : ICartService
    {
        private readonly ProductDbContext _context;
        private readonly IDistributedCache _distributedCache;
        private const string CartCacheKeyPrefix = "cart:items:";

        public CartService(ProductDbContext context, IDistributedCache distributedCache)
        {
            _context = context;
            _distributedCache = distributedCache;
        }

        public async Task<IEnumerable<CartItemDto>> GetCartItemsAsync(int customerId)
        {
            var cacheKey = $"{CartCacheKeyPrefix}{customerId}";
            var cachedData = await _distributedCache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
                return JsonSerializer.Deserialize<List<CartItemDto>>(cachedData)!;

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.CustomerId == customerId)
                .Select(c => new CartItemDto
                {
                    Id = c.Id,
                    Product = c.Product,
                    Quantity = c.Quantity
                })
                .ToListAsync();

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };

            await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cartItems), cacheOptions);
            return cartItems;
        }

        public async Task<bool> AddToCartAsync(int customerId, int productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    CustomerId = customerId,
                    ProductId = productId,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            await _distributedCache.RemoveAsync($"{CartCacheKeyPrefix}{customerId}");
            return true;
        }

        public async Task<bool> RemoveCartItemAsync(int customerId, int id)
        {
            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);
            if (item == null) return false;

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            await _distributedCache.RemoveAsync($"{CartCacheKeyPrefix}{customerId}");
            return true;
        }

        public async Task<bool> ClearCartAsync(int customerId)
        {
            var items = _context.CartItems.Where(c => c.CustomerId == customerId);
            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
            await _distributedCache.RemoveAsync($"{CartCacheKeyPrefix}{customerId}");
            return true;
        }

        public async Task<bool> UpdateCartItemQuantityAsync(int customerId, UpdateCartItemDto dto)
        {
            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == dto.CartItemId && c.CustomerId == customerId);
            if (item == null) return false;

            item.Quantity = dto.Quantity;
            await _context.SaveChangesAsync();
            await _distributedCache.RemoveAsync($"{CartCacheKeyPrefix}{customerId}");
            return true;
        }
    }
}
