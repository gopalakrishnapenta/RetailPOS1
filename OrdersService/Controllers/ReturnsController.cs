using Microsoft.AspNetCore.Mvc;
using OrdersService.Data;
using OrdersService.Models;

using Microsoft.AspNetCore.Authorization;
using OrdersService.Interfaces;

namespace OrdersService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReturnsController : ControllerBase
    {
        private readonly IReturnService _returnService;

        public ReturnsController(IReturnService returnService)
        {
            _returnService = returnService;
        }

        [HttpGet]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _returnService.GetAllReturnsAsync());
        }

        [HttpPost("initiate")]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> Initiate([FromBody] Return returnRequest)
        {
            var result = await _returnService.InitiateReturnAsync(returnRequest);
            return Ok(new { message = "Return initiated successfully", returnRequest = result });
        }

        [HttpPost("{id}/approve")]
        [Authorize(Policy = "StoreManagerOrHigher")]
        public async Task<IActionResult> Approve(int id, [FromBody] string? note)
        {
            var success = await _returnService.ApproveReturnAsync(id, note);
            if (!success) return BadRequest(new { message = "Return could not be approved. Ensure it exists AND is in 'Initiated' status." });
            return Ok(new { message = "Return approved and stock restock event published" });
        }

        [HttpPost("{id}/reject")]
        [Authorize(Policy = "StoreManagerOrHigher")]
        public async Task<IActionResult> Reject(int id, [FromBody] string? note)
        {
            var success = await _returnService.RejectReturnAsync(id, note);
            if (!success) return BadRequest(new { message = "Return could not be rejected. Ensure it exists AND is in 'Initiated' status." });
            return Ok(new { message = "Return rejected successfully" });
        }
    }
}
