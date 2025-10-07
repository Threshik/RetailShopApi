using RetailShopApi.Models.Entity;

namespace RetailShopApi.Models.DTOs
{
    public class CartItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int Quantity { get; set; }
    }
}
