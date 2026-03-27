using Microsoft.AspNetCore.Mvc;
using OrdersService.Interfaces;
using OrdersService.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace OrdersService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "Staff")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        [Authorize(Policy = "StoreManagerOrHigher")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _customerService.GetAllCustomersAsync());
        }

        [HttpGet("{mobile}")]
        public async Task<IActionResult> GetByMobile(string mobile)
        {
            var customer = await _customerService.GetByMobileAsync(mobile);
            if (customer == null) return NotFound();
            return Ok(customer);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate([FromBody] CustomerDto customerDto)
        {
            var result = await _customerService.CreateOrUpdateCustomerAsync(customerDto);
            return Ok(new { message = "Customer processed successfully", customer = result });
        }
    }
}
