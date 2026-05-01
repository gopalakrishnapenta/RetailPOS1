namespace RetailPOS.Contracts
{
    public interface PaymentProcessedEvent
    {
        Guid? CorrelationId { get; }
        int PaymentId { get; }
        int OrderId { get; }
        decimal Amount { get; }
        string PaymentMode { get; }
        string Status { get; } 
    }

    public interface PaymentFailedEvent
    {
        int OrderId { get; }
        string Reason { get; }
    }

    public interface PaymentRefundedEvent
    {
        int OrderId { get; }
        int? ReturnId { get; }
        int PaymentId { get; }
        decimal RefundedAmount { get; }
    }
}
