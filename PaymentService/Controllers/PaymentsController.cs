using Microsoft.AspNetCore.Mvc;
using PaymentService.Models;
using PaymentService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using RetailPOS.Common.Authorization;

namespace PaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("collect")]
        [Authorize(Policy = Permissions.Orders.Finalize)]
        public async Task<IActionResult> Collect([FromBody] Payment payment)
        {
            var result = await _paymentService.ProcessPaymentAsync(payment);
            return Ok(new { message = "Payment collected successfully", payment = result });
        }
    }
}
