using PaymentService.Interfaces;
using PaymentService.Models;
using MassTransit;
using RetailPOS.Contracts;

namespace PaymentService.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPublishEndpoint _publishEndpoint;

        public PaymentService(IPaymentRepository paymentRepository, IPublishEndpoint publishEndpoint)
        {
            _paymentRepository = paymentRepository;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<Payment> ProcessPaymentAsync(Payment payment)
        {
            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            // Publish event so OrdersService can update Bill status to "Paid"
            await _publishEndpoint.Publish<PaymentProcessedEvent>(new
            {
                PaymentId = payment.Id,
                OrderId = payment.BillId,
                Amount = payment.Amount,
                PaymentMode = payment.PaymentMode,
                Status = "Success"
            });

            return payment;
        }

        public async Task<IEnumerable<Payment>> GetStorePaymentsAsync(int storeId)
        {
            return await _paymentRepository.FindAsync(p => p.StoreId == storeId);
        }
    }
}
