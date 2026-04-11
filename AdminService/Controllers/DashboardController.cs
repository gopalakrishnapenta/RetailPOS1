using Microsoft.AspNetCore.Mvc;
using AdminService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using RetailPOS.Common.Authorization;

namespace AdminService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("stats")]
        [Authorize(Policy = RetailPOS.Common.Authorization.Permissions.Admin.ReportsView)]
        public async Task<IActionResult> GetStats([FromQuery] int? storeId = null)
        {
            return Ok(await _dashboardService.GetDashboardAsync(storeId));
        }
    }
}
