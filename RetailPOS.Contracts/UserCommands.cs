namespace RetailPOS.Contracts
{
    public interface CreateStaffProfileCommand
    {
        int UserId { get; }
        string Email { get; }
        string? FullName { get; }
        string? RoleName { get; }
        int? StoreId { get; }
    }

    public interface StaffProfileCreatedEvent
    {
        int UserId { get; }
    }
}
