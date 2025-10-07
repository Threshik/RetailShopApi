using Microsoft.EntityFrameworkCore;
using RetailShopApi.Data;
using RetailShopApi.Models.DTOs;
using RetailShopApi.Models.Entity;
using RetailShopApi.Services.Interfaces;

namespace RetailShopApi.Services.Implementation
{
    public class OrderService(ProductDbContext context) : IOrderService
    {
        private readonly ProductDbContext _context = context;

        public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Name,
                        Price = oi.Product.Price,
                        Quantity = oi.Quantity
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<bool> PlaceOrderAsync()
        {
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .ToListAsync();

            if (cartItems == null || !cartItems.Any())
                return false;

            var totalAmount = cartItems.Sum(item => item.Product.Price * item.Quantity);

            var newOrder = new Order
            {
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalAmount,
                OrderItems = new List<OrderItem>()
            };

            foreach (var cartItem in cartItems)
            {
                newOrder.OrderItems.Add(new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity
                });
            }

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return true;
        
    }
    }
}
