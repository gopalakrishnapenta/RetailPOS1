using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using RetailPOS.Contracts;

namespace OrdersService.Consumers
{
    public class PaymentProcessedConsumer : IConsumer<PaymentProcessedEvent>
    {
        private readonly OrdersDbContext _context;
        private readonly ILogger<PaymentProcessedConsumer> _logger;

        public PaymentProcessedConsumer(OrdersDbContext context, ILogger<PaymentProcessedConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Processing payment success for Order: {OrderId}", message.OrderId);

            // Fetch the bill. We use IgnoreQueryFilters because background consumers don't have a HttpContext tenant.
            var bill = await _context.Bills
                .IgnoreQueryFilters() 
                .FirstOrDefaultAsync(b => b.Id == message.OrderId);

            if (bill != null)
            {
                bill.Status = "Paid";
                await _context.SaveChangesAsync();
                _logger.LogInformation("Bill {BillNumber} marked as Paid.", bill.BillNumber);
            }
            else
            {
                _logger.LogWarning("Bill with ID {OrderId} not found for payment processing.", message.OrderId);
            }
        }
    }
}
