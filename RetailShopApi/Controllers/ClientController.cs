using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RetailShopApi.Services;

namespace RetailShopApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientController : ControllerBase
    {
        private readonly KeycloakService _keyCloakService;

        public ClientController (KeycloakService keycloakService)
        {
            _keyCloakService = keycloakService;
        }

        [HttpPost("create-client")]
        public async Task<IActionResult> CreateClient([FromBody] ClientRequest request)
        {
            if (string.IsNullOrEmpty(request.ClientId) || string.IsNullOrEmpty(request.RedirectUri))
                return BadRequest("ClientId and RedirectUri are required.");

            var result = await _keyCloakService.CreateClientAsync(request.ClientId, request.RedirectUri);

            if (result == null)
                return BadRequest("Failed to create client.");

            return Ok(new { message = "Client created successfully", clientId = request.ClientId });
        }

        public class ClientRequest
        {
            public string ClientId { get; set; } = string.Empty;
            public string RedirectUri { get; set; } = string.Empty;
        }

    }
}
