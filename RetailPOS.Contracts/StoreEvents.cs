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
}
