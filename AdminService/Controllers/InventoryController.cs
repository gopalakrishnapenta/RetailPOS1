using Microsoft.AspNetCore.Mvc;
using AdminService.Interfaces;
using AdminService.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace AdminService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "StoreManagerOrHigher")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpPost("adjustments")]
        public async Task<IActionResult> Adjust([FromBody] InventoryAdjustmentDto adjustmentDto)
        {
            var success = await _inventoryService.AdjustInventoryAsync(adjustmentDto);
            return Ok(new { message = "Inventory adjustment saved successfully" });
        }

        [HttpGet("adjustments")]
        public async Task<IActionResult> GetAdjustments([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            return Ok(await _inventoryService.GetAdjustmentsAsync(page, pageSize));
        }
    }
}
