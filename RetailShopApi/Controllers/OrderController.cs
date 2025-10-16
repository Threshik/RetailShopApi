using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailShopApi.Data;
using RetailShopApi.Models.DTOs;
using RetailShopApi.Services.Interfaces;
using System.Security.Claims;

namespace RetailShopApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ProductDbContext _context;

        public OrderController(IOrderService orderService, ProductDbContext context)
        {
            _orderService = orderService;
            _context = context;
        }

        //get customer ID from Keycloak token → map to DB record
        private async Task<int> GetCustomerIdAsync()
        {
            var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(keycloakId))
                throw new Exception("Keycloak ID not found in token.");

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.KeycloakId == keycloakId);

            if (customer == null)
                throw new Exception("Customer not found in database for this Keycloak user.");

            return customer.Id;
            
        }



        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            var customerId = await GetCustomerIdAsync(); // Get from Keycloak token

            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product) // Include product details
                .Where(o => o.CustomerId == customerId)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    CustomerId = o.CustomerId,
                    Customer = o.Customer,
                    OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.Product != null ? oi.Product.Name : "Unknown Product",
                        Price = oi.Product != null ? oi.Product.Price : 0,
                        Quantity = oi.Quantity
                    }).ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }






        [HttpPost("place")]
        public async Task<IActionResult> PlaceOrder()
        {
            var customerId = await GetCustomerIdAsync();
            var success = await _orderService.PlaceOrderAsync(customerId);

            if (!success)
                return BadRequest(new { message = "Your cart is empty." });

            return Ok(new { message = "Order placed successfully." });
        }
    }
}
