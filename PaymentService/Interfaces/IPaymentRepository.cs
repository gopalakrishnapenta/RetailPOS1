using PaymentService.Models;

namespace PaymentService.Interfaces
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        // Add specific methods for Payments here if needed
    }
}
