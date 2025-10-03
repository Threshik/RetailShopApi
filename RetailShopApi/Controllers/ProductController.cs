using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RetailShopApi.Data;

namespace RetailShopApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ProductDbContext _context;

        public ProductController(ProductDbContext context)
        {
            _context = context;

        }
        [HttpGet]


    }
}
