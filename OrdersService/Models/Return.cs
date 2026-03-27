using System.ComponentModel.DataAnnotations;

namespace OrdersService.Models
{
    public class Return
    {
        [Key]
        public int Id { get; set; }

        public int OriginalBillId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Status { get; set; } = "Initiated"; // Initiated, Approved, Refunded

        [MaxLength(500)]
        public string ManagerApprovalNote { get; set; } = string.Empty;

        public int StoreId { get; set; }
    }
}
