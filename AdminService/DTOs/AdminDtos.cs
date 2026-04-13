namespace AdminService.DTOs
{
    public class DashboardDto
    {
        public decimal TotalSales { get; set; } // Net Sales
        public decimal TodaySales { get; set; } // Today's Net
        public decimal GrossSales { get; set; } // Total before refunds
        public decimal RefundedAmount { get; set; } // Total approved refunds
        public int TotalBills { get; set; }
        public int TodayBills { get; set; }
        public double SalesChangePercent { get; set; }
        public int ActiveCashiers { get; set; }
        public int LowStockAlerts { get; set; }
        public List<RecentBillDto> RecentBills { get; set; } = new();
        public List<LowStockItemDto> LowStockItems { get; set; } = new();
        public List<HourlyTrendDto> HourlyTrend { get; set; } = new();
        public List<CategoryBreakdownDto> CategoryBreakdown { get; set; } = new(); // Used for "Share" chart
        public List<StoreMatrixDisplayDto> StoreMatrix { get; set; } = new();
        public List<StaffLeaderboardDto> StaffLeaderboard { get; set; } = new();
    }

    public class StoreMatrixDisplayDto
    {
        public string StoreName { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public decimal Refunds { get; set; }
        public int Transactions { get; set; }
        public int SharePercent { get; set; }
    }

    public class StaffLeaderboardDto
    {
        public string StaffName { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public int Orders { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    public class RecentBillDto
    { 
        public int Id { get; set; }
        public string Customer { get; set; } = string.Empty; 
        public decimal Total { get; set; } 
        public string Time { get; set; } = string.Empty;
    }
    public class LowStockItemDto 
    {
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty; 
        public int Stock { get; set; } 
    }
    public class HourlyTrendDto
    {
        public string Hour { get; set; } = string.Empty;
        public decimal Sales { get; set; }
    }
    public class CategoryBreakdownDto
    {
        public string Category { get; set; } = string.Empty; 
        public int Percent { get; set; } 
    }

    public class InventoryAdjustmentDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string ReasonCode { get; set; } = string.Empty;
        public string DocumentReference { get; set; } = string.Empty;
        public DateTime AdjustmentDate { get; set; }
    }

    public class InventoryItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public string Status => StockQuantity > 10 ? "In Stock" : (StockQuantity > 0 ? "Low Stock" : "Out of Stock");
    }

    public class SalesReportDto
    {
        public string Date { get; set; } = string.Empty;
        public int Bills { get; set; }
        public decimal Sales { get; set; }
        public decimal Tax { get; set; }
    }

    public class TaxReportDto
    {
        public decimal TotalTaxCollected { get; set; }
        public List<TaxPeriodDto> TaxByPeriod { get; set; } = new();
    }

    public class TaxPeriodDto 
    {
        public string Period { get; set; } = string.Empty;
        public decimal Amount { get; set; } 
    }

    public class PaginatedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
