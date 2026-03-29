using Microsoft.AspNetCore.Mvc;
using OrdersService.Interfaces;
using OrdersService.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace OrdersService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "Staff")]
    public class BillsController : ControllerBase
    {
        private readonly IBillService _billService;

        public BillsController(IBillService billService)
        {
            _billService = billService;
        }

        [HttpGet]
        [Authorize(Policy = "StoreManagerOrHigher")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _billService.GetAllBillsAsync());
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "Staff")]
        public async Task<IActionResult> GetById(int id)
        {
            var bill = await _billService.GetBillByIdAsync(id);
            return Ok(bill);
        }

        [HttpPost("cart/items")]
        public async Task<IActionResult> CreateOrUpdateCart([FromBody] BillDto cartDto)
        {
            var result = await _billService.CreateOrUpdateCartAsync(cartDto);
            return Ok(new { message = "Cart saved successfully", bill = result });
        }

        [HttpPost("{id}/finalize")]
        public async Task<IActionResult> Finalize(int id)
        {
            await _billService.FinalizeBillAsync(id);
            return Ok(new { message = "Bill finalized successfully" });
        }

        [HttpPost("{id}/hold")]
        public async Task<IActionResult> Hold(int id)
        {
            await _billService.HoldBillAsync(id);
            return Ok(new { message = "Bill held successfully" });
        }
    }
}
