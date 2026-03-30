namespace PaymentService.Interfaces
{
    public interface ITenantProvider
    {
        int StoreId { get; }
        string Role { get; }
        string? Token { get; }
    }
}
