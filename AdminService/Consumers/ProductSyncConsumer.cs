using MassTransit;
using RetailPOS.Contracts;
using AdminService.Data;
using AdminService.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Consumers
{
    public class ProductSyncConsumer : IConsumer<ProductCreatedEvent>, IConsumer<ProductUpdatedEvent>
    {
        private readonly AdminDbContext _context;
        private readonly ILogger<ProductSyncConsumer> _logger;

        public ProductSyncConsumer(AdminDbContext context, ILogger<ProductSyncConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming ProductCreatedEvent for Product {data.ProductId}: {data.Name}");

            try
            {
                var existing = await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == data.ProductId);
                
                // DATA RECONCILIATION: If we are 'Initial Syncing' this product, clear old unreliable history 
                // in the dashboard database for this specific product so it resets to the Catalog source.
                var oldAdjustments = await _context.InventoryAdjustments.IgnoreQueryFilters()
                    .Where(a => a.ProductId == data.ProductId)
                    .ToListAsync();
                if (oldAdjustments.Any())
                {
                    _context.InventoryAdjustments.RemoveRange(oldAdjustments);
                    _logger.LogWarning($"Cleared {oldAdjustments.Count} legacy stock adjustments for Product {data.ProductId} during sync.");
                }

                if (existing == null)
                {
                    var product = new SyncedProduct
                    {
                        Id = data.ProductId,
                        Name = data.Name,
                        Sku = data.Sku,
                        StoreId = data.StoreId
                    };
                    _context.Products.Add(product);
                }
                else
                {
                    existing.Name = data.Name;
                    existing.Sku = data.Sku;
                    existing.StoreId = data.StoreId;
                }

                // Record the fresh source-of-truth stock
                var adjustment = new InventoryAdjustment
                {
                    ProductId = data.ProductId,
                    StoreId = data.StoreId,
                    Quantity = data.InitialStock,
                    ReasonCode = "SyncReset",
                    DocumentReference = "Manual Catalog Sync",
                    AdjustmentDate = DateTime.UtcNow,
                    IsApproved = true
                };
                _context.InventoryAdjustments.Add(adjustment);

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Successfully synced product {data.Name} (ID: {data.ProductId}) with fresh stock {data.InitialStock}. Dashboard reset completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to handle ProductCreatedEvent for ID {data.ProductId}");
            }
        }

        public async Task Consume(ConsumeContext<ProductUpdatedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming ProductUpdatedEvent for Product {data.ProductId}");

            try
            {
                var existing = await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == data.ProductId);
                if (existing == null) return;

                existing.Name = data.Name;
                existing.Sku = data.Sku;

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Updated synced product {existing.Name} (ID: {existing.Id}).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to handle ProductUpdatedEvent for ID {data.ProductId}");
            }
        }
    }

    public class StockSyncConsumer : IConsumer<StockAdjustedEvent>
    {
        private readonly AdminDbContext _context;
        private readonly ILogger<StockSyncConsumer> _logger;

        public StockSyncConsumer(AdminDbContext context, ILogger<StockSyncConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<StockAdjustedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming StockAdjustedEvent for Product {data.ProductId}, Change: {data.QuantityChange}");

            try
            {
                var adjustment = new InventoryAdjustment
                {
                    ProductId = data.ProductId,
                    StoreId = data.StoreId,
                    Quantity = data.QuantityChange,
                    ReasonCode = data.ReasonCode,
                    DocumentReference = data.DocumentReference,
                    AdjustmentDate = DateTime.UtcNow,
                    IsApproved = true
                };

                _context.InventoryAdjustments.Add(adjustment);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Recorded manual stock adjustment for Product {data.ProductId} in Dashboard.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to handle StockAdjustedEvent for Product {data.ProductId}");
            }
        }
    }
}
