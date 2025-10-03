using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RetailShopApi.Data;
using Microsoft.EntityFrameworkCore;
using RetailShopApi.Models;

namespace RetailShopApi.Controllers
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        //dependency injection of the repository
        private readonly ProductDbContext _context;
        public ProductsController(ProductDbContext context)
        {
            _context = context;

        }

        [HttpGet]
       public async Task<ActionResult<IEnumerable<Product>>> GetAllProducts()
        {
            return await _context.Products.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return product;
        }

        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProductById), 
                new { id = product.Id }, 
                product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, Product updatedProduct)
        {
            if(id!=updatedProduct.Id)
            {
                return BadRequest();
            }

            var product = await _context.Products.FindAsync(id);
            if(product==null)
            {
                return NotFound();
            }

            product.Name = updatedProduct.Name;
            product.Description = updatedProduct.Description;
            product.Price = updatedProduct.Price;
            product.Image = updatedProduct.Image;
            await _context.SaveChangesAsync();
            return Ok(product);

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if(product==null)
            {
                return NotFound();
            }
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }

    }
}
