namespace RetailPOS.Contracts
{
    public interface PaymentProcessedEvent
    {
        int PaymentId { get; }
        int OrderId { get; }
        decimal Amount { get; }
        string PaymentMode { get; }
        string Status { get; } // Success, Failed
    }

    public interface PaymentFailedEvent
    {
        int OrderId { get; }
        string Reason { get; }
    }
}
