using Microsoft.AspNetCore.Mvc;
using AdminService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using RetailPOS.Common.Authorization;

namespace AdminService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("sales")]
        [Authorize(Policy = RetailPOS.Common.Authorization.Permissions.Admin.ReportsView)]
        public async Task<IActionResult> GetSalesReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _reportService.GetSalesReportAsync(from, to);
            return Ok(result);
        }

        [HttpGet("tax")]
        [Authorize(Policy = RetailPOS.Common.Authorization.Permissions.Admin.ReportsView)]
        public async Task<IActionResult> GetTaxReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _reportService.GetTaxReportAsync(from, to);
            return Ok(result);
        }
    }
}
