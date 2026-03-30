namespace RetailPOS.Contracts
{
    public interface UserRegisteredEvent
    {
        int UserId { get; }
        string Email { get; }
    }

    public interface StaffAssignedEvent
    {
        int UserId { get; }
        int StoreId { get; }
        string RoleName { get; }
    }
}
