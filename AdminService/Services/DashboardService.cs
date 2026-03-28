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
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public DashboardService(IHttpClientFactory httpFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpFactory.CreateClient();
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<DashboardDto> GetDashboardAsync()
        {
            // Fetch data from other services
            var bills = await FetchAsync<List<InternalBillDto>>("http://127.0.0.1:5003/api/bills") ?? new();
            var products = await FetchAsync<List<InternalProductDto>>("http://127.0.0.1:5002/api/products") ?? new();
            var customers = await FetchAsync<List<InternalCustomerDto>>("http://127.0.0.1:5003/api/customers") ?? new();

            var customerMap = customers.GroupBy(c => c.Mobile, StringComparer.OrdinalIgnoreCase)
                                       .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);
            var today = DateTime.UtcNow.Date;
            var todayBills = bills.Where(b => b.Status == "Finalized" && b.Date.Date == today).ToList();
            var yesterdayBills = bills.Where(b => b.Status == "Finalized" && b.Date.Date == today.AddDays(-1)).ToList();
            var finalizedBills = bills.Where(b => b.Status == "Finalized").ToList();

            var todaySales = todayBills.Sum(b => b.TotalAmount);
            var yesterdaySales = yesterdayBills.Sum(b => b.TotalAmount);
            double salesChange = yesterdaySales > 0 ? Math.Round((double)((todaySales - yesterdaySales) / yesterdaySales * 100), 1) : 0;

            var recentBills = bills.OrderByDescending(b => b.Date).Take(5).Select(b => new RecentBillDto
            {
                Id = b.Id,
                Customer = !string.IsNullOrEmpty(b.CustomerName) ? b.CustomerName :
                          (!string.IsNullOrEmpty(b.CustomerMobile) && customerMap.TryGetValue(b.CustomerMobile, out var cName)) ? cName : 
                          (string.IsNullOrEmpty(b.CustomerMobile) ? "Walking Customer" : "Guest"),
                Total = b.TotalAmount,
                Time = b.Date.ToLocalTime().ToString("t")
            }).ToList();

            var lowStockProds = products.Where(p => p.StockQuantity <= p.ReorderLevel).ToList();
            var lowStockItems = lowStockProds.OrderBy(p => p.StockQuantity).Take(5).Select(p => new LowStockItemDto
            {
                Name = p.Name, SKU = p.Sku, Stock = p.StockQuantity
            }).ToList();

            var hourlyData = todayBills.GroupBy(b => b.Date.ToLocalTime().Hour).OrderBy(g => g.Key)
                .Select(g => new HourlyTrendDto { Hour = $"{g.Key:D2}:00", Sales = g.Sum(b => b.TotalAmount) }).ToList();

            var categoryMap = new Dictionary<int, string> { { 1, "Electronics" }, { 2, "Grocery" }, { 3, "Beverages" }, { 4, "Fruits" }, { 5, "Veggies" } };
            var catRevenue = products.GroupBy(p => categoryMap.GetValueOrDefault(p.CategoryId, "Other")).Select(g => new { Category = g.Key, Count = g.Count() }).ToList();
            var totalProds = catRevenue.Sum(c => c.Count);
            var categoryBreakdown = catRevenue.OrderByDescending(c => c.Count).Select(c => new CategoryBreakdownDto {
                Category = c.Category, Percent = totalProds > 0 ? (int)Math.Round((double)c.Count / totalProds * 100) : 0
            }).Where(c => c.Percent > 0).ToList();

            return new DashboardDto
            {
                TotalSales = finalizedBills.Sum(b => b.TotalAmount), TodaySales = todaySales,
                TotalBills = finalizedBills.Count, TodayBills = todayBills.Count,
                SalesChangePercent = salesChange, ActiveCashiers = 1, LowStockAlerts = lowStockProds.Count,
                RecentBills = recentBills, LowStockItems = lowStockItems, HourlyTrend = hourlyData, CategoryBreakdown = categoryBreakdown
            };
        }

        private async Task<T?> FetchAsync<T>(string url)
        {
            try 
            {
                var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader);
                }

                // Propagate X-Store-Id header for Admin context
                var storeIdHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-Store-Id"].ToString();
                if (!string.IsNullOrEmpty(storeIdHeader))
                {
                    _httpClient.DefaultRequestHeaders.Remove("X-Store-Id");
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Store-Id", storeIdHeader);
                }

                return await _httpClient.GetFromJsonAsync<T>(url, _jsonOpts);
            } 
            catch { return default; }
        }

        // Internal records for mapping
        record InternalBillDto(int Id, string? CustomerMobile, string? CustomerName, decimal TotalAmount, decimal TaxAmount, string? Status, DateTime Date);
        record InternalProductDto(int Id, string Name, string Sku, int StockQuantity, int ReorderLevel, int CategoryId);
        record InternalCustomerDto(string Mobile, string Name);
    }
}
