using MassTransit;
using RetailPOS.Contracts;
using CatalogService.Interfaces;
using CatalogService.DTOs;

namespace CatalogService.Consumers
{
    public class StockAdjustedConsumer : IConsumer<StockAdjustedEvent>
    {
        private readonly IProductService _productService;
        private readonly ILogger<StockAdjustedConsumer> _logger;

        public StockAdjustedConsumer(IProductService productService, ILogger<StockAdjustedConsumer> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<StockAdjustedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming StockAdjustedEvent for Product {data.ProductId}: {data.QuantityChange} ({data.ReasonCode})");

            try
            {
                var updateDto = new StockUpdateDto
                {
                    ProductId = data.ProductId,
                    Quantity = Math.Abs(data.QuantityChange)
                };

                var list = new List<StockUpdateDto> { updateDto };

                if (data.QuantityChange > 0)
                {
                    await _productService.AddStockAsync(list);
                    _logger.LogInformation($"Successfully added stock for Product {data.ProductId}");
                }
                else
                {
                    await _productService.DeductStockAsync(list);
                    _logger.LogInformation($"Successfully deducted stock for Product {data.ProductId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process stock adjustment for Product {data.ProductId}");
                throw; // Retry via MassTransit
            }
        }
    }
}
