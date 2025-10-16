namespace RetailShopApi.Models.Entity
{
    public class Customer
    {
        public int Id { get; set; }

        public required string Username { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Gender { get; set; }
        public required string PasswordHash { get; set; }
        public string? KeycloakId { get; set; }

        public List<Order> Orders { get; set; } = new();
        public List<CartItem> CartItems { get; set; } = new();
    }
}
