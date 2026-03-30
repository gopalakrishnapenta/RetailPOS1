using Microsoft.AspNetCore.Mvc;
using PaymentService.Models;
using PaymentService.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace PaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "Staff")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("collect")]
        public async Task<IActionResult> Collect([FromBody] Payment payment)
        {
            var result = await _paymentService.ProcessPaymentAsync(payment);
            return Ok(new { message = "Payment collected successfully", payment = result });
        }
    }
}
