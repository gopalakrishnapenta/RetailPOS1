using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using RetailPOS.Contracts;

namespace OrdersService.Consumers
{
    public class OrderReturnedConsumer : IConsumer<OrderReturnedEvent>
    {
        private readonly OrdersDbContext _context;
        private readonly ILogger<OrderReturnedConsumer> _logger;

        public OrderReturnedConsumer(OrdersDbContext context, ILogger<OrderReturnedConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderReturnedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Processing order return for Order: {OrderId}", message.OrderId);

            // Fetch the bill. We use IgnoreQueryFilters because background consumers don't have a HttpContext tenant.
            var bill = await _context.Bills
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == message.OrderId);

            if (bill != null)
            {
                bill.Status = "Returned"; 
                await _context.SaveChangesAsync();
                _logger.LogInformation("Bill {BillNumber} marked as Returned (Restock completed in Catalog).", bill.BillNumber);
            }
            else
            {
                _logger.LogWarning("Bill with ID {OrderId} not found for return completion.", message.OrderId);
            }
        }
    }
}
