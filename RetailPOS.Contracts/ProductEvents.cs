namespace RetailPOS.Contracts
{
    public interface ProductCreatedEvent
    {
        int ProductId { get; }
        string Name { get; }
        string Sku { get; }
        int InitialStock { get; }
        int StoreId { get; }
    }

    public interface ProductUpdatedEvent
    {
        int ProductId { get; }
        string Name { get; }
        string Sku { get; }
    }
}
