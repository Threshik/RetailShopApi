using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailShopApi.Data;
using RetailShopApi.Models;

namespace RetailShopApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ProductDbContext _context;

        public CartController(ProductDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CartItem>>> GetCartItems()
        {
            return await _context.CartItems.Include( c=> c.Product).ToListAsync();
        }

        [HttpPost("add")]
        public async Task <IActionResult> AddToCart([FromQuery] int productId, [FromQuery] int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound(new { error = "Product Not Found" });
            }

            var cartItem = new CartItem
            {
                ProductId = productId,
                Quantity = quantity
            };

            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync();


            return Ok(new {message = "Added to cart"});
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveCartItem(int id)
        {
            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem == null)
            {
                return NotFound();
            }
            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var allItems = _context.CartItems;
            _context.CartItems.RemoveRange(allItems);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateCartItemQuantity([FromBody] UpdateCartItemDto dto)
        {
            var cartItem = await _context.CartItems.FindAsync(dto.CartItemId);
            if (cartItem == null)
            {
                return NotFound(new { error = "Cart item not found." });
            }

            cartItem.Quantity = dto.Quantity;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cart item updated." });
        }
        public class UpdateCartItemDto
        {
            public int CartItemId { get; set; }
            public int Quantity { get; set; }
        }
    }
}
