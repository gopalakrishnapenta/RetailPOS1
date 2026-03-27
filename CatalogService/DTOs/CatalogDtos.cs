namespace CatalogService.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal MRP { get; set; }
        public decimal SellingPrice { get; set; }
        public string TaxCode { get; set; } = string.Empty;
        public int ReorderLevel { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int StoreId { get; set; }
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
    
    public class StockUpdateDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
