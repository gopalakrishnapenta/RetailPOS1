using Razorpay.Api;
using PaymentService.Interfaces;
using PaymentService.DTOs;
using Microsoft.Extensions.Options;

namespace PaymentService.Services
{
    public class RazorpayService : IRazorpayService
    {
        private readonly IConfiguration _config;
        private readonly string _key;
        private readonly string _secret;

        public RazorpayService(IConfiguration config)
        {
            _config = config;
            _key = _config["Razorpay:KeyId"] ?? "rzp_test_placeholder_key";
            _secret = _config["Razorpay:KeySecret"] ?? "placeholder_secret";
        }

        public async Task<RazorpayOrderResponse> CreateOrderAsync(RazorpayOrderRequest request)
        {
            return await Task.Run(() =>
            {
                var client = new RazorpayClient(_key, _secret);

                Dictionary<string, object> options = new Dictionary<string, object>();
                options.Add("amount", (int)(request.Amount * 100)); // Convert to paise
                options.Add("currency", request.Currency);
                options.Add("receipt", request.Receipt);

                Order order = client.Order.Create(options);

                return new RazorpayOrderResponse
                {
                    OrderId = order["id"].ToString(),
                    Amount = request.Amount,
                    Currency = request.Currency,
                    KeyId = _key
                };
            });
        }

        public bool VerifyPaymentSignature(RazorpayVerifyRequest verifyRequest)
        {
            try
            {
                var client = new RazorpayClient(_key, _secret);

                Dictionary<string, string> attributes = new Dictionary<string, string>();
                attributes.Add("razorpay_order_id", verifyRequest.RazorpayOrderId);
                attributes.Add("razorpay_payment_id", verifyRequest.RazorpayPaymentId);
                attributes.Add("razorpay_signature", verifyRequest.RazorpaySignature);

                Utils.verifyPaymentSignature(attributes);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
