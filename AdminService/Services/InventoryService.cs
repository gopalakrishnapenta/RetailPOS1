using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using AdminService.Data;
using AdminService.Models;
using AdminService.DTOs;
using AdminService.Interfaces;

namespace AdminService.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryAdjustmentRepository _adjustmentRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;

        public InventoryService(IInventoryAdjustmentRepository adjustmentRepository, IHttpClientFactory httpFactory, IHttpContextAccessor httpContextAccessor)
        {
            _adjustmentRepository = adjustmentRepository;
            _httpClient = httpFactory.CreateClient();
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> AdjustInventoryAsync(InventoryAdjustmentDto dto)
        {
            var adjustment = new InventoryAdjustment
            {
                ProductId = dto.ProductId, 
                Quantity = dto.Quantity,
                ReasonCode = dto.ReasonCode, 
                DocumentReference = dto.DocumentReference, 
                AdjustmentDate = DateTime.UtcNow,
                StoreId = 1 // Default
            };
            await _adjustmentRepository.AddAsync(adjustment);
            await _adjustmentRepository.SaveChangesAsync();

            try
            {
                int qtyChange = adjustment.Quantity;
                if (adjustment.ReasonCode.Equals("Damage", StringComparison.OrdinalIgnoreCase) || adjustment.ReasonCode.Equals("Outward", StringComparison.OrdinalIgnoreCase))
                    qtyChange = -Math.Abs(adjustment.Quantity);
                else if (adjustment.ReasonCode.Equals("Inward", StringComparison.OrdinalIgnoreCase))
                    qtyChange = Math.Abs(adjustment.Quantity);

                var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader);
                }

                await _httpClient.PostAsJsonAsync("http://localhost:5002/api/products/adjust-stock", new { ProductId = adjustment.ProductId, QuantityChange = qtyChange });
            } catch { }
            return true;
        }

        public async Task<PaginatedResult<InventoryAdjustmentDto>> GetAdjustmentsAsync(int page = 1, int pageSize = 5)
        {
            var query = _adjustmentRepository.GetQueryable().AsNoTracking();
            var totalCount = await _adjustmentRepository.CountAsync(a => true);
            
            var items = await query
                .OrderByDescending(a => a.AdjustmentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new InventoryAdjustmentDto
                {
                    ProductId = a.ProductId, 
                    Quantity = a.Quantity, 
                    ReasonCode = a.ReasonCode,
                    DocumentReference = a.DocumentReference, 
                    AdjustmentDate = a.AdjustmentDate
                }).ToListAsync();

            return new PaginatedResult<InventoryAdjustmentDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }
    }
}
