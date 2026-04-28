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
            Console.WriteLine($"[DIAGNOSTIC] PaymentService: Saving payment to DB for Bill {payment.BillId}...");
            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();
            Console.WriteLine($"[DIAGNOSTIC] PaymentService: DB Save Successful. Publishing event...");
            
            try 
            {
                // Publish event so OrdersService can update Bill status to "Paid"
                await _publishEndpoint.Publish<PaymentProcessedEvent>(new
                {
                    PaymentId = payment.Id,
                    OrderId = payment.BillId,
                    Amount = payment.Amount,
                    PaymentMode = payment.PaymentMode,
                    Status = "Success"
                });
                Console.WriteLine($"[DIAGNOSTIC] PaymentService: Event published successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] PaymentService: Failed to publish event, but payment was saved: {ex.Message}");
                // We continue because the payment IS saved in the DB
            }

            return payment;
        }

        public async Task<IEnumerable<Payment>> GetStorePaymentsAsync(int storeId)
        {
            return await _paymentRepository.FindAsync(p => p.StoreId == storeId);
        }
    }
}
