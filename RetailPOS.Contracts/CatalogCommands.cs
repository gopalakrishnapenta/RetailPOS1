namespace RetailPOS.Contracts
{
    public interface DeductStockCommand
    {
        int OrderId { get; }
        List<DeductStockItem> Items { get; }
    }

    public interface DeductStockItem
    {
        int ProductId { get; }
        int Quantity { get; }
    }

    public interface StockDeductedEvent
    {
        int OrderId { get; }
    }

    public interface StockDeductionFailedEvent
    {
        int OrderId { get; }
        string Reason { get; }
    }

    public interface RestockItemCommand
    {
        int OrderId { get; }
        List<RestockItem> Items { get; }
    }

    public interface RestockItem
    {
        int ProductId { get; }
        int Quantity { get; }
    }

    public interface StockRestockedEvent
    {
        int OrderId { get; }
    }
}
