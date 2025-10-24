using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailShopApi.Data;
using RetailShopApi.Models.DTOs;
using RetailShopApi.Models.Entity;
using RetailShopApi.Services;
using System.Security.Cryptography;
using System.Text;

namespace RetailShopApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ProductDbContext _context;
        private readonly KeycloakService _keycloakService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(ProductDbContext context, KeycloakService keycloakService, ILogger<CustomerController> logger)
        {
            _context = context;
            _keycloakService = keycloakService;
            _logger = logger;
        }


        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterCustomer([FromBody] RegisterCustomerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.Password != dto.ConfirmPassword)
                return BadRequest(new { error = "Passwords do not match." });

            if (await _context.Customers.AnyAsync(c => c.Username == dto.Username || c.Email == dto.Email))
                return BadRequest(new { error = "Username or Email already exists." });

            try
            {
                //Create user in Keycloak
                var keycloakId = await _keycloakService.CreateUserAsync(
     dto.Username,
     dto.Email,
     dto.FirstName,
     dto.LastName,
     dto.Password,
     dto.PhoneNumber,
     dto.Gender
 );


                if (string.IsNullOrEmpty(keycloakId))
                {
                    _logger.LogError("Keycloak user creation failed for {Username}", dto.Username);
                    return StatusCode(500, new { error = "Failed to create user in Keycloak." });
                }

                // Save to local DB
                var customer = new Customer
                {
                    Username = dto.Username,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Gender = dto.Gender,
                    PasswordHash = HashPassword(dto.Password),
                    KeycloakId = keycloakId
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                var response = new CustomerDto
                {
                    Id = customer.Id,
                    Username = customer.Username,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Email = customer.Email,
                    PhoneNumber = customer.PhoneNumber,
                    Gender = customer.Gender
                };

                _logger.LogInformation("Customer {Username} registered successfully with Keycloak ID {KeycloakId}",
                    customer.Username, keycloakId);

                return Ok(new
                {
                    message = "Customer registered successfully.",
                    customer = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering customer {Username}", dto.Username);
                return StatusCode(500, new { error = "An unexpected error occurred during registration." });
            }
        }
        [AllowAnonymous]
        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleCustomerDto dto)
        {
            // Check if user already exists
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Username == dto.Username || c.Email == dto.Email);

            if (customer == null)
            {
                // Map DTO → Entity
                customer = new Customer
                {
                    Username = dto.Username,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber ?? "",
                    Gender = dto.Gender ?? "",
                    PasswordHash = "", // No password for Google login
                    KeycloakId = dto.KeycloakId
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            // Return minimal info back to Angular
            var response = new CustomerDto
            {
                Id = customer.Id,
                Username = customer.Username,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber,
                Gender = customer.Gender
            };

            return Ok(response);
        }




        // Hash password securely
        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
