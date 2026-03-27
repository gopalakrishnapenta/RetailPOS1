using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using AdminService.Data;
using AdminService.Models;
using AdminService.DTOs;
using AdminService.Interfaces;

namespace AdminService.Services
{
    public class ReportService : IReportService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReportService(IHttpClientFactory httpFactory, IHttpContextAccessor httpContextAccessor) 
        { 
            _httpClient = httpFactory.CreateClient(); 
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<SalesReportDto>> GetSalesReportAsync(DateTime? from, DateTime? to)
        {
            ApplyHeaders();
            var bills = await _httpClient.GetFromJsonAsync<List<InternalBillDto>>("http://localhost:5003/api/bills") ?? new();
            var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
            var toDate = to ?? DateTime.UtcNow;

            return bills.Where(b => b.Status == "Finalized" && b.Date >= fromDate && b.Date <= toDate)
                .GroupBy(b => b.Date.Date).OrderByDescending(g => g.Key)
                .Select(g => new SalesReportDto { Date = g.Key.ToString("yyyy-MM-dd"), Bills = g.Count(), Sales = g.Sum(b => b.TotalAmount - b.TaxAmount), Tax = g.Sum(b => b.TaxAmount) });
        }

        public async Task<TaxReportDto> GetTaxReportAsync(DateTime? from, DateTime? to)
        {
            ApplyHeaders();
            var bills = await _httpClient.GetFromJsonAsync<List<InternalBillDto>>("http://localhost:5003/api/bills") ?? new();
            var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
            var toDate = to ?? DateTime.UtcNow;

            var finalized = bills.Where(b => b.Status == "Finalized" && b.Date >= fromDate && b.Date <= toDate).ToList();
            return new TaxReportDto
            {
                TotalTaxCollected = finalized.Sum(b => b.TaxAmount),
                TaxByPeriod = finalized.GroupBy(b => b.Date.ToString("yyyy-MM"))
                    .Select(g => new TaxPeriodDto { Period = g.Key, Amount = g.Sum(b => b.TaxAmount) }).OrderByDescending(x => x.Period).ToList()
            };
        }

        private void ApplyHeaders()
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
        }

        record InternalBillDto(int Id, DateTime Date, decimal TotalAmount, decimal TaxAmount, string Status);
    }
}
