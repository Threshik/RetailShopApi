using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailShopApi.Data;
using RetailShopApi.Models;
using RetailShopApi.Models.DTOs;
using RetailShopApi.Services.Interfaces;

namespace RetailShopApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCartItems()
        {
            var items = await _cartService.GetCartItemsAsync();
            return Ok(items);
        }


        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromQuery] int productId, [FromQuery] int quantity)
        {
            var result = await _cartService.AddToCartAsync(productId, quantity);

            if (!result)
                return NotFound(new { error = "Product not found" });

            return Ok(new { message = "Added to cart" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveCartItem(int id)
        {
            var success = await _cartService.RemoveCartItemAsync(id);

            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            await _cartService.ClearCartAsync();
            return NoContent();
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateCartItemQuantity(UpdateCartItemDto dto)
        {
            var success = await _cartService.UpdateCartItemQuantityAsync(dto);

            if (!success)
                return NotFound(new { error = "Cart item not found" });

            return Ok(new { message = "Cart item updated" });
        }
    }
}

