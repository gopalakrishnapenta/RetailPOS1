using Microsoft.AspNetCore.Mvc;
using AdminService.Interfaces;
using AdminService.Models;
using AdminService.DTOs;
using Microsoft.AspNetCore.Authorization;
using RetailPOS.Common.Authorization;

namespace AdminService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpPost("adjust")]
        [Authorize(Policy = RetailPOS.Common.Authorization.Permissions.Admin.InventoryAdjust)]
        public async Task<IActionResult> Adjust([FromBody] InventoryAdjustmentDto adjustment)
        {
            await _inventoryService.AdjustInventoryAsync(adjustment);
            return Ok(new { message = "Stock adjusted successfully" });
        }

        [HttpGet]
        [Authorize(Policy = RetailPOS.Common.Authorization.Permissions.Admin.ReportsView)]
        public async Task<IActionResult> GetInventory()
        {
            var summary = await _inventoryService.GetInventorySummaryAsync();
            return Ok(summary);
        }
    }
}
