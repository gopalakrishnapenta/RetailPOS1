using MassTransit;

namespace OrdersService.Sagas
{
    public class CheckoutSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = default!;

        public int OrderId { get; set; }
        public int StoreId { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CustomerMobile { get; set; }
        
        // Track the current step outcome
        public int? PaymentId { get; set; }
        public string? PaymentStatus { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Items to deduct after payment
        public List<SagaOrderItem>? Items { get; set; }
    }

    public class SagaOrderItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
