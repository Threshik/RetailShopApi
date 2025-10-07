using Microsoft.EntityFrameworkCore;
using RetailShopApi.Data;
using RetailShopApi.Models.DTOs;
using RetailShopApi.Models.Entity;
using RetailShopApi.Services.Interfaces;

namespace RetailShopApi.Services.Implementation
{
    public class ProductService : IProductService
    {

        private readonly ProductDbContext _context;
        public ProductService(ProductDbContext context)
        {
            _context = context;
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

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                Image = product.Image,


            };
        }

        public  async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
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
            var product = await _context.Products.FindAsync(id);
            if (product == null) {
                return null;

            }
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                Image = product.Image,

            };
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

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                Image = product.Image,

            };
        }
    }
}
