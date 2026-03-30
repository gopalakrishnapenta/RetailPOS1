using PaymentService.Data;
using PaymentService.Models;
using PaymentService.Interfaces;

namespace PaymentService.Repositories
{
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(PaymentDbContext context) : base(context)
        {
        }
    }
}
