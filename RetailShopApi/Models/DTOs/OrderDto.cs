using RetailShopApi.Models.Entity;

namespace RetailShopApi.Models.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public List<OrderItemDto> OrderItems { get; set; } = new();
    }
}
