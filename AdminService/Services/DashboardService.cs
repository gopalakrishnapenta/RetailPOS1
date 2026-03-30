using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Net.Http.Json;
using AdminService.Data;
using AdminService.Models;
using AdminService.DTOs;
using AdminService.Interfaces;

namespace AdminService.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AdminDbContext _context;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(AdminDbContext context, ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DashboardDto> GetDashboardAsync()
        {
            var today = DateTime.UtcNow.Date;
            var startOfYesterday = today.AddDays(-1);

            // Fetch local synced data (already filtered by StoreId via Global Query Filter)
            var bills = await _context.SyncedOrders.AsNoTracking().ToListAsync();
            
            // For Low Stock Alerts, we still reference our local Categories/Inventory count if available
            // but we can also use a simplified count for now.
            var lowStockCount = await _context.InventoryAdjustments.CountAsync(a => a.Quantity < 10); // Simplified logic

            var todayBills = bills.Where(b => b.Date.Date == today).ToList();
            var yesterdayBills = bills.Where(b => b.Date.Date == startOfYesterday).ToList();

            var todaySales = todayBills.Sum(b => b.TotalAmount);
            var yesterdaySales = yesterdayBills.Sum(b => b.TotalAmount);
            var totalSales = bills.Sum(b => b.TotalAmount);

            double salesChange = yesterdaySales > 0 
                ? Math.Round((double)((todaySales - yesterdaySales) / yesterdaySales * 100), 1) 
                : 0;

            var recentBills = bills.OrderByDescending(b => b.Date).Take(5).Select(b => new RecentBillDto
            {
                Id = b.OrderId,
                Customer = b.CustomerMobile ?? "Walking Customer",
                Total = b.TotalAmount,
                Time = b.Date.ToLocalTime().ToString("t")
            }).ToList();

            var hourlyTrend = todayBills.GroupBy(b => b.Date.ToLocalTime().Hour).OrderBy(g => g.Key)
                .Select(g => new HourlyTrendDto { Hour = $"{g.Key:D2}:00", Sales = g.Sum(b => b.TotalAmount) }).ToList();

            return new DashboardDto
            {
                TotalSales = totalSales,
                TodaySales = todaySales,
                GrossSales = totalSales,
                RefundedAmount = 0, // Returns handled separately
                TotalBills = bills.Count,
                TodayBills = todayBills.Count,
                SalesChangePercent = salesChange,
                ActiveCashiers = 1,
                LowStockAlerts = lowStockCount,
                RecentBills = recentBills,
                HourlyTrend = hourlyTrend
            };
        }
    }
}
