namespace RetailPOS.Contracts
{
    public interface UserRegisteredEvent
    {
        int UserId { get; }
        string Email { get; }
        string? FullName { get; }
        string? RoleName { get; }
        int? StoreId { get; }
    }

    public interface StaffAssignedEvent
    {
        int UserId { get; }
        int StoreId { get; }
        string RoleName { get; }
    }
}
