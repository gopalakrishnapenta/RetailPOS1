using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using RetailPOS.Contracts;

namespace OrdersService.Consumers
{
    public class ReturnInitiatedConsumer : IConsumer<ReturnInitiatedEvent>
    {
        private readonly OrdersDbContext _context;
        private readonly ILogger<ReturnInitiatedConsumer> _logger;

        public ReturnInitiatedConsumer(OrdersDbContext context, ILogger<ReturnInitiatedConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ReturnInitiatedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Processing return initiation for Order: {OrderId}", message.OrderId);

            // Fetch the bill. We use IgnoreQueryFilters because background consumers don't have a HttpContext tenant.
            var bill = await _context.Bills
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == message.OrderId);

            if (bill != null)
            {
                // Mark the bill status to 'Returned' or 'PendingReturn' to prevent further processing
                bill.Status = "Returned"; 
                await _context.SaveChangesAsync();
                _logger.LogInformation("Bill {BillNumber} marked as Returned.", bill.BillNumber);
            }
            else
            {
                _logger.LogWarning("Bill with ID {OrderId} not found for return processing.", message.OrderId);
            }
        }
    }
}
