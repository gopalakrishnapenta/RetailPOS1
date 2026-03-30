using PaymentService.Models;

namespace PaymentService.Interfaces
{
    public interface IPaymentService
    {
        Task<Payment> ProcessPaymentAsync(Payment payment);
        Task<IEnumerable<Payment>> GetStorePaymentsAsync(int storeId);
    }
}
