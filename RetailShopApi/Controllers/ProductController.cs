using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RetailShopApi.Data;
using Microsoft.EntityFrameworkCore;

namespace RetailShopApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ProductDbContext _context;

        public ProductController(ProductDbContext context)
        {
            _context = context;

        }
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products.ToListAsync();
            return Ok(products);
        }

    }
}
