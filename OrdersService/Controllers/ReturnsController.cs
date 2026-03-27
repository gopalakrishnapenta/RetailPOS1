using Microsoft.AspNetCore.Mvc;
using OrdersService.Data;
using OrdersService.Models;

using Microsoft.AspNetCore.Authorization;

namespace OrdersService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "StoreManagerOrHigher")]
    public class ReturnsController : ControllerBase
    {
        private readonly OrdersDbContext _context;

        public ReturnsController(OrdersDbContext context)
        {
            _context = context;
        }

        [HttpPost("initiate")]
        public async Task<IActionResult> Initiate([FromBody] Return returnRequest)
        {
            returnRequest.Status = "Initiated";
            _context.Returns.Add(returnRequest);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Return initiated successfully", returnRequest });
        }
    }
}
