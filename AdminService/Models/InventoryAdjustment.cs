using System.ComponentModel.DataAnnotations;

namespace AdminService.Models
{
    public class InventoryAdjustment
    {
        [Key]
        public int Id { get; set; }

        public int StoreId { get; set; }
        
        public int ProductId { get; set; }

        public int Quantity { get; set; } // Can be positive (inward) or negative (outward)

        [Required]
        [MaxLength(50)]
        public string ReasonCode { get; set; } = string.Empty; // Damage, Inward, Audit, Return

        [MaxLength(100)]
        public string DocumentReference { get; set; } = string.Empty;

        public DateTime AdjustmentDate { get; set; } = DateTime.UtcNow;

        public int AdjustedByUserId { get; set; }

        public bool IsApproved { get; set; } = false;
    }
}
