using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminService.Data;

namespace AdminService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataRepairController : ControllerBase
    {
        private readonly AdminDbContext _context;
        private readonly ILogger<DataRepairController> _logger;

        public DataRepairController(AdminDbContext context, ILogger<DataRepairController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("reassign-orders")]
        public async Task<IActionResult> ReassignOrders([FromQuery] int oldId, [FromQuery] int newId)
        {
            _logger.LogWarning($"Data Repair: Reassigning all orders from Staff ID {oldId} to {newId}");
            
            var orders = await _context.SyncedOrders.IgnoreQueryFilters()
                .Where(o => o.CashierId == oldId)
                .ToListAsync();

            foreach (var order in orders)
            {
                order.CashierId = newId;
            }

            int count = await _context.SaveChangesAsync();
            
            return Ok(new { 
                Message = $"Successfully reassigned {count} orders.", 
                AffectedRecords = count 
            });
        }
    }
}
