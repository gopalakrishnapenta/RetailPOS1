using Microsoft.AspNetCore.Mvc;
using OrdersService.Data;
using OrdersService.Models;
using Microsoft.AspNetCore.Authorization;

namespace OrdersService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "Staff")]
    public class PaymentsController : ControllerBase
    {
        private readonly OrdersDbContext _context;

        public PaymentsController(OrdersDbContext context)
        {
            _context = context;
        }

        [HttpPost("collect")]
        public async Task<IActionResult> Collect([FromBody] Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Payment collected successfully", payment });
        }
    }
}
