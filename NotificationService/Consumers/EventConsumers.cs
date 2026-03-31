using MassTransit;
using RetailPOS.Contracts;
using NotificationService.Services;

namespace NotificationService.Consumers
{
    public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<UserRegisteredConsumer> _logger;

        public UserRegisteredConsumer(INotificationService notificationService, ILogger<UserRegisteredConsumer> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
        {
            var user = context.Message;
            _logger.LogInformation($"[CONSUMER] New User Registered: {user.Email}");

            // 1. Notify Admin via Email
            await _notificationService.SendEmailAsync("admin@gmail.com", "New User Registered", 
                $"User {user.Email} has registered. Please assign an appropriate Role and Store.");

            // 2. Push real-time notification
            await _notificationService.SendRealTimeNotificationAsync($"New staff member registered: {user.Email}");
        }
    }

    public class StockAdjustedConsumer : IConsumer<StockAdjustedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<StockAdjustedConsumer> _logger;

        public StockAdjustedConsumer(INotificationService notificationService, ILogger<StockAdjustedConsumer> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<StockAdjustedEvent> context)
        {
            var stock = context.Message;
            _logger.LogInformation($"[CONSUMER] Stock Adjusted: ProductID={stock.ProductId}, Change={stock.QuantityChange}");

            // Dummy logic for "Low Stock"
            if (stock.QuantityChange < 0) // Potentially a sale or manual reduction
            {
                // In a real scenario, we'd check the current quantity from the DB. 
                // For this demo, if the reason is 'LOW_STOCK' or if it's a significant drop:
                if (stock.ReasonCode == "LOW_STOCK")
                {
                    await _notificationService.SendEmailAsync("manager@gmail.com", "Low Stock Alert", 
                        $"Product ID {stock.ProductId} is running low on stock. Please restock immediately.");

                    await _notificationService.SendRealTimeNotificationAsync($"LOW STOCK ALERT: Product ID {stock.ProductId}");
                }
            }
        }
    }

    public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<OrderPlacedConsumer> _logger;

        public OrderPlacedConsumer(INotificationService notificationService, ILogger<OrderPlacedConsumer> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
        {
            var order = context.Message;
            _logger.LogInformation($"[CONSUMER] New Order Placed: ID={order.OrderId}, Amount={order.TotalAmount}");

            // 1. Send SMS to Customer
            if (!string.IsNullOrEmpty(order.CustomerMobile))
            {
                await _notificationService.SendSmsAsync(order.CustomerMobile, 
                    $"Dear Customer, your order #{order.OrderId} for ₹{order.TotalAmount} has been placed successfully!");
            }

            // 2. Notify Store via SignalR
            await _notificationService.SendRealTimeNotificationAsync($"ORDER RECEIVED: New bill #{order.OrderId} finalized for ₹{order.TotalAmount}.");
        }
    }

    public class ReturnInitiatedConsumer : IConsumer<ReturnInitiatedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<ReturnInitiatedConsumer> _logger;

        public ReturnInitiatedConsumer(INotificationService notificationService, ILogger<ReturnInitiatedConsumer> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ReturnInitiatedEvent> context)
        {
            var ret = context.Message;
            _logger.LogInformation($"[CONSUMER] Return Initiated: OrderID={ret.OrderId}, ReturnID={ret.ServiceReturnId}");

            // Notify specific store group via SignalR
            await _notificationService.SendRealTimeNotificationAsync(
                $"RETURN ALERT: A return has been initiated for Order #{ret.OrderId}. (ID: {ret.ServiceReturnId})",
                storeId: ret.StoreId
            );
        }
    }

    public class OrderReturnedConsumer : IConsumer<OrderReturnedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<OrderReturnedConsumer> _logger;

        public OrderReturnedConsumer(INotificationService notificationService, ILogger<OrderReturnedConsumer> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderReturnedEvent> context)
        {
            var ret = context.Message;
            _logger.LogInformation($"[CONSUMER] Order Returned: OrderID={ret.OrderId}, Refund={ret.RefundAmount}");

            // Send SMS to Customer
            if (!string.IsNullOrEmpty(ret.CustomerMobile))
            {
                await _notificationService.SendSmsAsync(ret.CustomerMobile, 
                    $"Dear Customer, your refund of ₹{ret.RefundAmount} for Order #{ret.OrderId} has been processed successfully.");
            }

            // Also Notify Store via SignalR
            await _notificationService.SendRealTimeNotificationAsync(
                $"REFUND PROCESSED: Order #{ret.OrderId} has been returned. Refund of ₹{ret.RefundAmount} issued.",
                storeId: ret.StoreId
            );
        }
    }
}
