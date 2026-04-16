namespace RetailPOS.Contracts
{
    public interface StockAdjustedEvent
    {
        int ProductId { get; }
        int StoreId { get; }
        int QuantityChange { get; }
        string ReasonCode { get; }
        string DocumentReference { get; }
    }
}
