namespace RetailShopApi.Models.DTOs
{
    public class RegisterCustomerDto
    {
        public required string Username { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Gender { get; set; }

        public required string Password { get; set; }
        public required string ConfirmPassword { get; set; }
    }
}
