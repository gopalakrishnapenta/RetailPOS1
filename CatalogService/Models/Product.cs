using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatalogService.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Sku { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Barcode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MRP { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }

        [MaxLength(20)]
        public string TaxCode { get; set; } = string.Empty;

        public int ReorderLevel { get; set; } = 10;

        public bool IsActive { get; set; } = true;
        
        public int StockQuantity { get; set; } = 0;

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int StoreId { get; set; }
    }
}
