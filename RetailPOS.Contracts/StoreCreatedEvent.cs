namespace RetailPOS.Contracts
{
    public interface StoreCreatedEvent
    {
        int Id { get; }
        string StoreCode { get; }
        string Name { get; }
    }

    public interface StoreUpdatedEvent
    {
        int Id { get; }
        string StoreCode { get; }
        string Name { get; }
    }

    public interface StoreDeletedEvent
    {
        int Id { get; }
        string StoreCode { get; }
    }

    public interface OrderPlacedEvent
    {
        int OrderId { get; }
        int StoreId { get; }
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
        List<ReturnedItemEvent> Items { get; }
    }

    public interface ReturnedItemEvent
    {
        int ProductId { get; }
        int Quantity { get; }
    }
}
