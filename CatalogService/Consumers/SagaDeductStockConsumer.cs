using MassTransit;
using RetailPOS.Contracts;
using CatalogService.Interfaces;

namespace CatalogService.Consumers
{
    public class SagaDeductStockConsumer : 
        IConsumer<DeductStockCommand>,
        IConsumer<RestockItemCommand>
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<SagaDeductStockConsumer> _logger;

        public SagaDeductStockConsumer(IProductRepository productRepository, ILogger<SagaDeductStockConsumer> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<DeductStockCommand> context)
        {
            var command = context.Message;
            _logger.LogInformation($"Processing DeductStockCommand for Order {command.OrderId}");

            try
            {
                foreach (var item in command.Items)
                {
                    var product = await _productRepository.GetByIdIgnoringFiltersAsync(item.ProductId);
                    if (product == null)
                    {
                        throw new Exception($"Product {item.ProductId} not found.");
                    }

                    if (product.StockQuantity < item.Quantity)
                    {
                        throw new Exception($"Insufficient stock for product {product.Name}. Available: {product.StockQuantity}, Requested: {item.Quantity}");
                    }

                    product.StockQuantity -= item.Quantity;
                    _productRepository.Update(product);
                }

                await _productRepository.SaveChangesAsync();
                
                await context.Publish<StockDeductedEvent>(new { OrderId = command.OrderId });
                _logger.LogInformation($"Stock deducted successfully for Order {command.OrderId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to deduct stock for Order {command.OrderId}");
                await context.Publish<StockDeductionFailedEvent>(new 
                { 
                    OrderId = command.OrderId, 
                    Reason = ex.Message 
                });
            }
        }

        public async Task Consume(ConsumeContext<RestockItemCommand> context)
        {
            var command = context.Message;
            _logger.LogInformation($"Processing RestockItemCommand for Order {command.OrderId}");

            try
            {
                foreach (var item in command.Items)
                {
                    var product = await _productRepository.GetByIdIgnoringFiltersAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                        _productRepository.Update(product);
                    }
                }

                await _productRepository.SaveChangesAsync();
                await context.Publish<StockRestockedEvent>(new { OrderId = command.OrderId });
                _logger.LogInformation($"Stock restocked successfully for Order {command.OrderId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restock items.");
            }
        }
    }
}
