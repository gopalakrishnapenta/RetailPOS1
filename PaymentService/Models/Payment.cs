using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentService.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        public int BillId { get; set; }

        [Required]
        [MaxLength(20)]
        public string PaymentMode { get; set; } = "Cash"; // Cash, Card, UPI

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(100)]
        public string ReferenceNumber { get; set; } = string.Empty;

        public int StoreId { get; set; }
    }
}
