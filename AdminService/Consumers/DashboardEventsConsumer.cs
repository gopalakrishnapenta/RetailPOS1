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
                    CashierId = data.CashierId,
                    Date = data.Date,
                    CustomerMobile = data.CustomerMobile
                };

                _context.SyncedOrders.Add(syncedOrder);
                
                // SYNC INVENTORY: Record stock deduction for the dashboard's stock summary
                foreach (var item in data.Items)
                {
                    var adjustment = new InventoryAdjustment
                    {
                        StoreId = data.StoreId,
                        ProductId = item.ProductId,
                        Quantity = -item.Quantity, // Negative because it's a sale
                        ReasonCode = "Sales",
                        DocumentReference = $"Order #{data.OrderId}",
                        AdjustmentDate = data.Date,
                        IsApproved = true
                    };
                    _context.InventoryAdjustments.Add(adjustment);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Synced Order {data.OrderId} and its inventory adjustments for Dashboard.");
            }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && (sqlEx.Number == 2627 || sqlEx.Number == 2601))
            {
                _logger.LogWarning($"Order {data.OrderId} was already synced by another process. Skipping.");
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
                // Check if this return is already synced
                var existing = await _context.SyncedReturns.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.ReturnId == data.ReturnId);
                if (existing != null) return;

                var syncedReturn = new SyncedReturn
                {
                    OrderId = data.OrderId,
                    ReturnId = data.ReturnId,
                    StoreId = data.StoreId,
                    RefundAmount = data.RefundAmount,
                    Date = DateTime.UtcNow
                };

                _context.SyncedReturns.Add(syncedReturn);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Synced Return {data.ReturnId} for Order {data.OrderId} to Dashboard.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process return event for Order {data.OrderId}");
            }
        }
    }
}
