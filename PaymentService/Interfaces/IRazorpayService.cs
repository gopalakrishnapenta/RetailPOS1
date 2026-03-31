using PaymentService.DTOs;

namespace PaymentService.Interfaces
{
    public interface IRazorpayService
    {
        Task<RazorpayOrderResponse> CreateOrderAsync(RazorpayOrderRequest request);
        bool VerifyPaymentSignature(RazorpayVerifyRequest verifyRequest);
    }
}
