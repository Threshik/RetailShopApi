using Microsoft.AspNetCore.Mvc;
using RetailShopApi.DTOs;

namespace RetailShopApi.Services.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllProductsAsync();
        Task<ProductDto> GetProductByIdAsync(int id);
        Task<ProductDto> CreateProductAsync(CreateProductDto dto);
        Task<ProductDto> UpdateProductAsync(int id, CreateProductDto dto);
        Task<bool> DeleteProductAsync(int id);

    }
}
