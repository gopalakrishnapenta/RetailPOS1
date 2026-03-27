namespace RetailPOS.Contracts
{
    public interface StoreCreatedEvent
    {
        int Id { get; }
        string StoreCode { get; }
        string Name { get; }
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
}
