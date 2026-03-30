using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrdersService.Models
{
    public class Bill
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string BillNumber { get; set; } = string.Empty;

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public int StoreId { get; set; }
        
        public int CashierId { get; set; }

        [MaxLength(20)]
        public string CustomerMobile { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Draft"; // Draft, Held, Finalized

        public string CustomerName { get; set; } = string.Empty;

        public ICollection<BillItem> Items { get; set; } = new List<BillItem>();
    }
}
