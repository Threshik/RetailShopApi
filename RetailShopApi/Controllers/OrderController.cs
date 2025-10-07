using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailShopApi.Data;
using RetailShopApi.Models;
using RetailShopApi.Services.Interfaces;

namespace RetailShopApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }

        [HttpPost("place")]
        public async Task<IActionResult> PlaceOrder()
        {

            var success = await _orderService.PlaceOrderAsync();
            if (!success)
                return BadRequest(new { message = "Cart is empty." });

            return Ok(new { message = "Order placed successfully." });
        }
    }
}
