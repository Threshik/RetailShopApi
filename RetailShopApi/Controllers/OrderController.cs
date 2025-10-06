using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailShopApi.Data;
using RetailShopApi.Models;

namespace RetailShopApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly ProductDbContext _context;

        public OrderController(ProductDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
        {
            return await _context.Orders
                .Include(o => o.OrderItems)           // include order items
                .ThenInclude(oi => oi.Product)        // and include the product info inside each item
                .ToListAsync();
        }

        [HttpPost("place")]
        public async Task<IActionResult> PlaceOrder()
        {
            
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .ToListAsync();

            
            if (cartItems == null || !cartItems.Any())
            {
                return BadRequest(new { message = "Cart is empty." });
            }

            
            var totalAmount = cartItems.Sum(item => item.Product.Price * item.Quantity);

            
            var newOrder = new Order
            {
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalAmount,
                OrderItems = new List<OrderItem>() 
            };

            
            foreach (var cartItem in cartItems)
            {
                var orderItem = new OrderItem
                {
                    ProductId = cartItem.ProductId,     
                    Quantity = cartItem.Quantity,
                    Product = cartItem.Product,         
                    Order = newOrder                    
                };

                newOrder.OrderItems.Add(orderItem);     
            }

            
            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order placed successfully." });
        }
    }
}
