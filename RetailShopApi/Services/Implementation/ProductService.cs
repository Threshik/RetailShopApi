using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RetailShopApi.Data;
using RetailShopApi.Models.DTOs;
using RetailShopApi.Models.Entity;
using RetailShopApi.Services.Interfaces;
using System.Text.Json;

namespace RetailShopApi.Services.Implementation
{
    public class ProductService : IProductService
    {

        private readonly ProductDbContext _context;
        private readonly IDistributedCache _distributedCache;
        public ProductService(ProductDbContext context, IDistributedCache distributedCache)
        {
            _context = context;
            _distributedCache = distributedCache;
        }
        public async Task<ProductDto> CreateProductAsync(ProductDto dto)
        {
            var product = new Product
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Image = dto.Image,

            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var resultDto=  new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                Image = product.Image,


            };

            string cacheKey = $"product:{resultDto.Id}";
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(resultDto), cacheOptions);

            return resultDto;
        }

        public  async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            string cacheKey = $"product:{id}";
            await _distributedCache.RemoveAsync(cacheKey);
            return true;
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            return await _context.Products
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Description = p.Description,
                    Image = p.Image,
                    
                })
                .ToListAsync();
        }

        public async Task<ProductDto> GetProductByIdAsync(int id)
        {
            string cacheKey = $"product:{id}";
            string cachedProduct = await _distributedCache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedProduct)){
                return JsonSerializer.Deserialize<ProductDto>(cachedProduct);
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null) {
                return null;

            }
            var dto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                Image = product.Image,

            };
             var cacheOptions = new DistributedCacheEntryOptions
             {
                 AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
             };
            await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), cacheOptions);

            return dto;
        }

        public async Task<ProductDto> UpdateProductAsync(int id, CreateProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return null;

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Image = dto.Image;

            await _context.SaveChangesAsync();

            var resultDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                Image = product.Image,

            };
            string cacheKey = $"product:{id}";
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(resultDto), cacheOptions);

            return resultDto;
        }
    }
}
