using MassTransit;
using RetailPOS.Contracts;
using PaymentService.Interfaces;
using PaymentService.Models;

namespace PaymentService.Consumers
{
    public class CheckoutSagaCommandsConsumer : 
        IConsumer<ProcessPaymentCommand>,
        IConsumer<RefundPaymentCommand>
    {
        private readonly IPaymentService _paymentService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<CheckoutSagaCommandsConsumer> _logger;

        public CheckoutSagaCommandsConsumer(IPaymentService paymentService, IPublishEndpoint publishEndpoint, ILogger<CheckoutSagaCommandsConsumer> logger)
        {
            _paymentService = paymentService;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ProcessPaymentCommand> context)
        {
            var command = context.Message;
            _logger.LogInformation($"Processing ProcessPaymentCommand for Order {command.OrderId}");

            // In a real system, you'd integrate with a gateway like Razorpay here.
            // For now, we simulate success as per current logic.
            var payment = new Payment
            {
                BillId = command.OrderId,
                Amount = command.Amount,
                PaymentMode = command.PaymentMode,
                ReferenceNumber = $"SAGA-{command.OrderId}-{Guid.NewGuid().ToString().Substring(0,8)}"
            };

            await _paymentService.ProcessPaymentAsync(payment);
            
            _logger.LogInformation($"Payment processed for Order {command.OrderId}");
        }

        public async Task Consume(ConsumeContext<RefundPaymentCommand> context)
        {
            var command = context.Message;
            _logger.LogInformation($"Processing RefundPaymentCommand for Order {command.OrderId}, Amount: {command.RefundAmount}");

            // Simulate refund logic
            await _publishEndpoint.Publish<PaymentRefundedEvent>(new
            {
                OrderId = command.OrderId,
                ReturnId = command.ReturnId,
                PaymentId = command.PaymentId,
                RefundedAmount = command.RefundAmount
            });

            _logger.LogInformation($"Refund processed for Order {command.OrderId}");
        }
    }
}
