namespace RetailPOS.Contracts
{
    public interface StockAdjustedEvent
    {
        int ProductId { get; }
        int QuantityChange { get; }
        string ReasonCode { get; }
    }
}
