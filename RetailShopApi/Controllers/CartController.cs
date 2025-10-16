using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailShopApi.Data; // ✅ for AppDbContext
using RetailShopApi.Models.DTOs;
using RetailShopApi.Services.Interfaces;
using System;
using System.Security.Claims;

namespace RetailShopApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ProductDbContext _context; 

        public CartController(ICartService cartService, ProductDbContext context)
        {
            _cartService = cartService;
            _context = context;
        }

        /// <summary>
        /// Gets the internal CustomerId (int) based on the Keycloak ID (GUID) from the token.
        /// </summary>
        private int GetCustomerId()
        {
            var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(keycloakId))
                throw new Exception("Keycloak ID not found in token.");

            var customer = _context.Customers.FirstOrDefault(c => c.KeycloakId == keycloakId);

            if (customer == null)
                throw new Exception("Customer not found in database for this Keycloak user.");

            return customer.Id;
        }

        [HttpGet]
        public async Task<IActionResult> GetCartItems()
        {
            var customerId = GetCustomerId();
            var items = await _cartService.GetCartItemsAsync(customerId);
            return Ok(items);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromQuery] int productId, [FromQuery] int quantity)
        {
            var customerId = GetCustomerId();
            var result = await _cartService.AddToCartAsync(customerId, productId, quantity);

            if (!result)
                return NotFound(new { error = "Product not found" });

            return Ok(new { message = "Added to cart" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveCartItem(int id)
        {
            var customerId = GetCustomerId();
            var success = await _cartService.RemoveCartItemAsync(customerId, id);

            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var customerId = GetCustomerId();
            await _cartService.ClearCartAsync(customerId);
            return NoContent();
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateCartItemQuantity(UpdateCartItemDto dto)
        {
            var customerId = GetCustomerId();
            var success = await _cartService.UpdateCartItemQuantityAsync(customerId, dto);

            if (!success)
                return NotFound(new { error = "Cart item not found" });

            return Ok(new { message = "Cart item updated" });
        }
    }
}
