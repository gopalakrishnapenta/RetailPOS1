using MassTransit;
using RetailPOS.Contracts;
using OrdersService.Data;
using OrdersService.Models;
using Microsoft.EntityFrameworkCore;

namespace OrdersService.Consumers
{
    public class PaymentProcessedConsumer : IConsumer<PaymentProcessedEvent>
    {
        private readonly OrdersDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<PaymentProcessedConsumer> _logger;

        public PaymentProcessedConsumer(OrdersDbContext context, IPublishEndpoint publishEndpoint, ILogger<PaymentProcessedConsumer> logger)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming PaymentProcessedEvent for Order {data.OrderId}, Status: {data.Status}");

            try
            {
                // Find the bill (ignoring tenant filters to ensuring background processing reaches the record)
                var bill = await _context.Bills.IgnoreQueryFilters()
                    .Include(b => b.Items)
                    .FirstOrDefaultAsync(b => b.Id == data.OrderId);
                if (bill == null)
                {
                    _logger.LogWarning($"Bill {data.OrderId} not found for payment update.");
                    return;
                }

                if (data.Status == "Success")
                {
                    _logger.LogInformation($"Payment Success for Bill {bill.Id}. Triggering stock and dashboard updates.");

                    // 1. Update Bill Status locally
                    bill.Status = "Finalized"; 
                    await _context.SaveChangesAsync();

                    // 2. NOW trigger the global synchronization events
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

                    _logger.LogInformation($"Successfully synchronized Order {bill.Id} after payment.");
                }
                else
                {
                    _logger.LogWarning($"Payment Failed for Bill {bill.Id}. Reverting to Draft state.");
                    bill.Status = "Draft"; // Let cashier try again
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing payment update for order {data.OrderId}");
            }
        }
    }
}
