using MassTransit;
using RetailPOS.Contracts;
using CatalogService.Models;
using CatalogService.Interfaces;

namespace CatalogService.Consumers
{
    public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<OrderPlacedConsumer> _logger;

        public OrderPlacedConsumer(IProductRepository productRepository, ILogger<OrderPlacedConsumer> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming OrderPlacedEvent for OrderId: {data.OrderId}");

            foreach (var item in data.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    _logger.LogInformation($"Deducting {item.Quantity} from Product {product.Name} (Stock: {product.StockQuantity})");
                    product.StockQuantity -= item.Quantity;
                    _productRepository.Update(product);
                }
                else
                {
                    _logger.LogWarning($"Product with ID {item.ProductId} not found for Order {data.OrderId}");
                }
            }

            await _productRepository.SaveChangesAsync();
            _logger.LogInformation($"Stock updated successfully for Order {data.OrderId}");
        }
    }
}
