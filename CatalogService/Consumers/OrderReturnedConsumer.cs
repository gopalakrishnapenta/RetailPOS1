using MassTransit;
using RetailPOS.Contracts;
using CatalogService.Interfaces;

namespace CatalogService.Consumers
{
    public class OrderReturnedConsumer : IConsumer<OrderReturnedEvent>
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<OrderReturnedConsumer> _logger;

        public OrderReturnedConsumer(IProductRepository productRepository, ILogger<OrderReturnedConsumer> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderReturnedEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation($"Consuming OrderReturnedEvent for OrderId: {data.OrderId}");

            foreach (var item in data.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    _logger.LogInformation($"Restocking {item.Quantity} units for Product {product.Name} (Current Stock: {product.StockQuantity})");
                    product.StockQuantity += item.Quantity;
                    _productRepository.Update(product);
                }
                else
                {
                    _logger.LogWarning($"Product with ID {item.ProductId} not found while processing return for Order {data.OrderId}");
                }
            }

            await _productRepository.SaveChangesAsync();
            _logger.LogInformation($"Restocking completed successfully for Return {data.ReturnId}");
        }
    }
}
