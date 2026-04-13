using System.ComponentModel.DataAnnotations;

namespace AdminService.Models
{
    public class SyncedOrder
    {
        [Key]
        public int OrderId { get; set; }
        public int StoreId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public int CashierId { get; set; }
        public DateTime Date { get; set; }
        public string? CustomerMobile { get; set; }
    }

    public class DashboardStats
    {
        [Key]
        public int Id { get; set; } // Single record (Id=1)
        public decimal TotalSales { get; set; }
        public decimal TodaySales { get; set; }
        public int TotalBills { get; set; }
        public int TodayBills { get; set; }
        public int LowStockAlerts { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class SyncedReturn
    {
        [Key]
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ReturnId { get; set; }
        public decimal RefundAmount { get; set; }
        public int StoreId { get; set; }
        public DateTime Date { get; set; }
    }
}
