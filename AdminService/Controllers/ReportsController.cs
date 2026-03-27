using Microsoft.AspNetCore.Mvc;
using AdminService.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace AdminService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "StoreManagerOrHigher")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("sales")]
        public async Task<IActionResult> GetSalesReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _reportService.GetSalesReportAsync(from, to);
            return Ok(result);
        }

        [HttpGet("tax")]
        public async Task<IActionResult> GetTaxReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _reportService.GetTaxReportAsync(from, to);
            return Ok(result);
        }
    }
}
