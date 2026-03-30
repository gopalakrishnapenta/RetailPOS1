using Microsoft.AspNetCore.Mvc;
using ReturnsService.Models;
using ReturnsService.Services;
using ReturnsService.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace ReturnsService.Controllers
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
            await _returnService.ApproveReturnAsync(id, note);
            return Ok(new { message = "Return approved and stock restock event published" });
        }

        [HttpPost("{id}/reject")]
        [Authorize(Policy = "StoreManagerOrHigher")]
        public async Task<IActionResult> Reject(int id, [FromBody] string? note)
        {
            await _returnService.RejectReturnAsync(id, note);
            return Ok(new { message = "Return rejected successfully" });
        }
    }
}
