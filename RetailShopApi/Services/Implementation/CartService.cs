using Microsoft.EntityFrameworkCore;
using RetailShopApi.Data;
using RetailShopApi.Models.Entity;
using RetailShopApi.Models.DTOs;
using RetailShopApi.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace RetailShopApi.Services.Implementation
{
    

    public class CartService : ICartService

    {
        private readonly ProductDbContext _context;
        private readonly IDistributedCache _distributedCache;

        public CartService(ProductDbContext context, IDistributedCache distributedCache)
        {
            _context = context;
            _distributedCache = distributedCache;
        }
        private const string CartCacheKey = "cart:items";

        public async Task<IEnumerable<CartItemDto>> GetCartItemsAsync()
        {
            string cachedData = await _distributedCache.GetStringAsync(CartCacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                //json to object
                return JsonSerializer.Deserialize<List<CartItemDto>>(cachedData);
            }
            var cartItems =  await _context.CartItems
                .Include(c => c.Product)
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
            await _distributedCache.SetStringAsync(CartCacheKey, JsonSerializer.Serialize(cartItems), cacheOptions);
            return cartItems;
        }

        public async Task<bool> AddToCartAsync(int productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            var cartItem = new CartItem
            {
                ProductId = productId,
                Quantity = quantity
            };

            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync();
            await _distributedCache.RemoveAsync(CartCacheKey);
            return true;
        }

        public async Task<bool> RemoveCartItemAsync(int id)
        {
            var item = await _context.CartItems.FindAsync(id);
            if (item == null) return false;

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            await _distributedCache.RemoveAsync(CartCacheKey);
            return true;
        }
        public async Task<bool> ClearCartAsync()
        {
            var items = _context.CartItems;
            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
            await _distributedCache.RemoveAsync(CartCacheKey);
            return true;
        }
        public async Task<bool> UpdateCartItemQuantityAsync(UpdateCartItemDto dto)
        {
            var item = await _context.CartItems.FindAsync(dto.CartItemId);
            if (item == null) return false;

            item.Quantity = dto.Quantity;
            await _context.SaveChangesAsync();
            await _distributedCache.RemoveAsync(CartCacheKey);
            return true;
        }

        
    }
}
