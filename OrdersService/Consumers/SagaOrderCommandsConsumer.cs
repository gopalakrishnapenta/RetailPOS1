using MassTransit;
using RetailPOS.Contracts;
using OrdersService.Data;
using Microsoft.EntityFrameworkCore;

namespace OrdersService.Consumers
{
    public class SagaOrderCommandsConsumer : IConsumer<FinalizeOrderCommand>
    {
        private readonly OrdersDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<SagaOrderCommandsConsumer> _logger;

        public SagaOrderCommandsConsumer(OrdersDbContext context, IPublishEndpoint publishEndpoint, ILogger<SagaOrderCommandsConsumer> logger)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<FinalizeOrderCommand> context)
        {
            var command = context.Message;
            _logger.LogInformation($"Saga finalizing order {command.OrderId}");

            var bill = await _context.Bills.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == command.OrderId);
            if (bill != null)
            {
                bill.Status = "Finalized"; 
                await _context.SaveChangesAsync();
                
                // NOW publish the official OrderPlacedEvent for the rest of the system
                await _publishEndpoint.Publish<OrderPlacedEvent>(new
                {
                    OrderId = bill.Id,
                    StoreId = bill.StoreId,
                    TotalAmount = bill.TotalAmount,
                    TaxAmount = bill.TaxAmount,
                    Date = DateTime.UtcNow,
                    CustomerMobile = bill.CustomerMobile,
                    Items = bill.Items.Select(i => new { ProductId = i.ProductId, Quantity = i.Quantity }).ToList()
                });

                _logger.LogInformation($"Order {command.OrderId} marked as Finalized and OrderPlacedEvent published.");
            }
        }
    }
}
