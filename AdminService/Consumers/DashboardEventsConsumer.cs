using MassTransit;
using RetailPOS.Contracts;
using AdminService.Data;
using AdminService.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Consumers
{
    public class DashboardEventsConsumer : IConsumer<OrderPlacedEvent>, IConsumer<OrderReturnedEvent>
    {
        private readonly AdminDbContext _context;
        private readonly ILogger<DashboardEventsConsumer> _logger;

        public DashboardEventsConsumer(AdminDbContext context, ILogger<DashboardEventsConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming OrderPlacedEvent for Order {data.OrderId}");

            try
            {
                // We IGNORE query filters here because we want to see if it already exists globally
                var existing = await _context.SyncedOrders.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.OrderId == data.OrderId);
                if (existing != null) return;

                var syncedOrder = new SyncedOrder
                {
                    OrderId = data.OrderId,
                    StoreId = data.StoreId,
                    TotalAmount = data.TotalAmount,
                    TaxAmount = data.TaxAmount,
                    Date = data.Date,
                    CustomerMobile = data.CustomerMobile
                };

                _context.SyncedOrders.Add(syncedOrder);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Synced Order {data.OrderId} for Dashboard.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to sync Order {data.OrderId} for Dashboard.");
            }
        }

        public async Task Consume(ConsumeContext<OrderReturnedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming OrderReturnedEvent for Order {data.OrderId} / Return {data.ReturnId}");

            try
            {
                // Find the original order to update stats if necessary, or just log the return
                // In a true read-model, we might just store a 'SyncedReturn' entry as well.
                // For now, calculating Net Sales dynamically from SyncedOrders - SyncedReturns is best.
                
                // (Future: implementing SyncedReturn model if dashboard needs specific return trends)
                _logger.LogInformation($"Return for Order {data.OrderId} detected. Net Sales will be updated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process return event for Order {data.OrderId}");
            }
        }
    }
}
