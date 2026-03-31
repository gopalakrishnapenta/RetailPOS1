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
        private readonly IRazorpayService _razorpayService;

        public PaymentsController(IPaymentService paymentService, IRazorpayService razorpayService)
        {
            _paymentService = paymentService;
            _razorpayService = razorpayService;
        }

        [HttpPost("collect")]
        [Authorize(Policy = Permissions.Orders.Finalize)]
        public async Task<IActionResult> Collect([FromBody] Payment payment)
        {
            var result = await _paymentService.ProcessPaymentAsync(payment);
            return Ok(new { message = "Payment collected successfully", payment = result });
        }

        [HttpPost("create-order")]
        [Authorize(Policy = Permissions.Payments.CreateOrder)]
        public async Task<IActionResult> CreateOrder([FromBody] PaymentService.DTOs.RazorpayOrderRequest request)
        {
            var order = await _razorpayService.CreateOrderAsync(request);
            return Ok(order);
        }

        [HttpPost("verify")]
        [Authorize(Policy = Permissions.Payments.Verify)]
        public async Task<IActionResult> Verify([FromBody] PaymentService.DTOs.RazorpayVerifyRequest verifyRequest)
        {
            var isValid = _razorpayService.VerifyPaymentSignature(verifyRequest);
            if (!isValid) return BadRequest(new { message = "Invalid payment signature" });

            // In a real scenario, you would update the order status in the DB here
            return Ok(new { message = "Payment verified successfully" });
        }
    }
}
