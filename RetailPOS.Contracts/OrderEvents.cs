namespace RetailPOS.Contracts
{
    public interface CheckoutInitiatedEvent
    {
        Guid? CorrelationId { get; }
        int OrderId { get; }
        int StoreId { get; }
        int CashierId { get; }
        decimal TotalAmount { get; }
        decimal TaxAmount { get; }
        DateTime Date { get; }
        string? CustomerMobile { get; }
        List<OrderItemContract> Items { get; }
    }

    public interface OrderItemContract
    {
        int ProductId { get; }
        int Quantity { get; }
    }

    public interface OrderPlacedEvent
    {
        int OrderId { get; }
        int StoreId { get; }
        int CashierId { get; }
        decimal TotalAmount { get; }
        decimal TaxAmount { get; }
        DateTime Date { get; }
        string? CustomerMobile { get; }
        List<OrderItemEvent> Items { get; }
    }

    public interface OrderItemEvent
    {
        int ProductId { get; }
        int Quantity { get; }
    }

    public interface OrderReturnedEvent
    {
        int OrderId { get; }
        int ReturnId { get; }
        int StoreId { get; }
        decimal RefundAmount { get; }
        string? CustomerMobile { get; }
        List<ReturnedItemEvent> Items { get; }
    }

    public interface ReturnedItemEvent
    {
        int ProductId { get; }
        int Quantity { get; }
        decimal RefundAmount { get; }
    }

    public interface ReturnInitiatedEvent
    {
        int OrderId { get; }
        int ServiceReturnId { get; }
        int StoreId { get; }
        decimal TotalRefund { get; }
        string? CustomerMobile { get; }
        List<ReturnedItemEvent> Items { get; }
    }

    public interface FinalizeOrderCommand
    {
        int OrderId { get; }
    }

    public interface ReturnApprovedEvent
    {
        int ReturnId { get; }
        string Note { get; }
    }

    public interface ReturnRejectedEvent
    {
        int ReturnId { get; }
        string Reason { get; }
    }

    public interface FinalizeReturnCommand
    {
        int ReturnId { get; }
        string NewStatus { get; }
    }

    public interface CheckoutTimeout
    {
        int OrderId { get; }
    }
}
