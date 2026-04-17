namespace OrdersService.Interfaces
{
    public interface ITenantProvider
    {
        int StoreId { get; }
        int UserId { get; }
        string Role { get; }
        string? Token { get; }
    }
}
