namespace RetailShopApi.Models.DTOs
{
    public class GoogleCustomerDto
    {
        public required string Username { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }

        // Optional fields
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public string? KeycloakId { get; set; }
    }
}


