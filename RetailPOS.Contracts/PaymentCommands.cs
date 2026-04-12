namespace RetailPOS.Contracts
{
    public interface ProcessPaymentCommand
    {
        int OrderId { get; }
        decimal Amount { get; }
        string CustomerMobile { get; }
        string PaymentMode { get; }
    }

    public interface RefundPaymentCommand
    {
        int OrderId { get; }
        int ReturnId { get; }
        int PaymentId { get; }
        decimal RefundAmount { get; }
        string Reason { get; }
    }
}
