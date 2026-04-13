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

        public async Task<DashboardDto> GetDashboardAsync(int? storeId = null)
        {
            var today = DateTime.UtcNow.Date;
            var startOfYesterday = today.AddDays(-1);

            // Bypass global query filters to allow global admin view or specific store filtering
            var orderQuery = _context.SyncedOrders.IgnoreQueryFilters();
            var returnQuery = _context.SyncedReturns.IgnoreQueryFilters();
            var inventoryQuery = _context.InventoryAdjustments.IgnoreQueryFilters();

            if (storeId.HasValue && storeId.Value != 0)
            {
                orderQuery = orderQuery.Where(o => o.StoreId == storeId.Value);
                returnQuery = returnQuery.Where(r => r.StoreId == storeId.Value);
                inventoryQuery = inventoryQuery.Where(a => a.StoreId == storeId.Value);
            }

            var bills = await orderQuery.AsNoTracking().ToListAsync();
            var returns = await returnQuery.AsNoTracking().ToListAsync();
            
            // Calculate current stock levels from adjustments and find items < 10
            var stockSummary = await inventoryQuery.AsNoTracking()
                .GroupBy(a => a.ProductId)
                .Select(g => new { ProductId = g.Key, CurrentStock = g.Sum(a => a.Quantity) })
                .ToListAsync();

            var lowStockItems = stockSummary.Where(s => s.CurrentStock < 10).ToList();
            var lowStockCount = lowStockItems.Count;

            var lowStockDetails = lowStockItems.Select(s => new LowStockItemDto
            {
                Name = $"Product {s.ProductId}", // Placeholder or we could join with Catalog if we were in the same DB
                SKU = $"SKU-{s.ProductId:D4}",
                Stock = s.CurrentStock
            }).ToList();

            var todayBills = bills.Where(b => b.Date.Date == today).ToList();
            var yesterdayBills = bills.Where(b => b.Date.Date == startOfYesterday).ToList();

            var todayReturns = returns.Where(r => r.Date.Date == today).ToList();
            var yesterdayReturns = returns.Where(r => r.Date.Date == startOfYesterday).ToList();

            var todaySales = todayBills.Sum(b => b.TotalAmount) - todayReturns.Sum(r => r.RefundAmount);
            var yesterdaySales = yesterdayBills.Sum(b => b.TotalAmount) - yesterdayReturns.Sum(r => r.RefundAmount);
            
            var totalGrossSales = bills.Sum(b => b.TotalAmount);
            var totalRefunded = returns.Sum(r => r.RefundAmount);
            var totalNetSales = totalGrossSales - totalRefunded;

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
                .Select(g => new HourlyTrendDto { 
                    Hour = $"{g.Key:D2}:00", 
                    Sales = g.Sum(b => b.TotalAmount) - todayReturns.Where(r => r.Date.ToLocalTime().Hour == g.Key).Sum(r => r.RefundAmount)
                }).ToList();

            // Calculate Store-wise share (Appropriate Breakdown)
            var categoryBreakdown = new List<CategoryBreakdownDto>();
            if (storeId.HasValue && storeId.Value != 0)
            {
                categoryBreakdown.Add(new CategoryBreakdownDto { Category = $"Store {storeId.Value}", Percent = 100 });
            }
            else if (totalNetSales > 0)
            {
                var storeGroups = bills.GroupBy(b => b.StoreId)
                    .Select(g => new { 
                        StoreId = g.Key, 
                        NetSales = g.Sum(b => b.TotalAmount) - returns.Where(r => r.StoreId == g.Key).Sum(r => r.RefundAmount) 
                    }).ToList();

                foreach (var sg in storeGroups)
                {
                    int percent = (int)Math.Round((double)(sg.NetSales / totalNetSales * 100));
                    categoryBreakdown.Add(new CategoryBreakdownDto { Category = $"Store {sg.StoreId}", Percent = percent });
                }
            }

            return new DashboardDto
            {
                TotalSales = totalNetSales,
                TodaySales = todaySales,
                GrossSales = totalGrossSales,
                RefundedAmount = totalRefunded, 
                TotalBills = bills.Count,
                TodayBills = todayBills.Count,
                SalesChangePercent = salesChange,
                ActiveCashiers = 1,
                LowStockAlerts = lowStockCount,
                LowStockItems = lowStockDetails,
                RecentBills = recentBills,
                HourlyTrend = hourlyTrend,
                CategoryBreakdown = categoryBreakdown
            };
        }
    }
}
