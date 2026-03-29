namespace RetailPOS.Contracts
{
    public interface CategoryCreatedEvent
    {
        int Id { get; }
        string Name { get; }
        string Description { get; }
        bool IsActive { get; }
        int StoreId { get; }
    }

    public interface CategoryUpdatedEvent
    {
        int Id { get; }
        string Name { get; }
        string Description { get; }
        bool IsActive { get; }
        int StoreId { get; }
    }

    public interface CategoryDeletedEvent
    {
        int Id { get; }
    }
}

