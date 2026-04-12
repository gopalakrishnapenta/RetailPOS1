using MassTransit;

namespace ReturnsService.Sagas
{
    public class ReturnSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = default!;

        public int ReturnId { get; set; }
        public int OrderId { get; set; }
        public int StoreId { get; set; }
        public decimal RefundAmount { get; set; }
        
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        
        public string? CustomerMobile { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
