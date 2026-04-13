using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using AdminService.Data;
using AdminService.Models;
using AdminService.Interfaces;
using AdminService.Exceptions;
using AdminService.DTOs;
using MassTransit;
using RetailPOS.Contracts;

namespace AdminService.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryAdjustmentRepository _adjustmentRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ITenantProvider _tenantProvider;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(IInventoryAdjustmentRepository adjustmentRepository, 
            IPublishEndpoint publishEndpoint, 
            ITenantProvider tenantProvider,
            ILogger<InventoryService> logger)
        {
            _adjustmentRepository = adjustmentRepository;
            _publishEndpoint = publishEndpoint;
            _tenantProvider = tenantProvider;
            _logger = logger;
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
                StoreId = _tenantProvider.StoreId
            };
            await _adjustmentRepository.AddAsync(adjustment);
            await _adjustmentRepository.SaveChangesAsync();

            // ── Event-Driven Synchronization ──────────────────────────────────
            int qtyChange = adjustment.Quantity;
            if (adjustment.ReasonCode.Equals("Damage", StringComparison.OrdinalIgnoreCase) || adjustment.ReasonCode.Equals("Outward", StringComparison.OrdinalIgnoreCase))
                qtyChange = -Math.Abs(adjustment.Quantity);
            else if (adjustment.ReasonCode.Equals("Inward", StringComparison.OrdinalIgnoreCase))
                qtyChange = Math.Abs(adjustment.Quantity);

            await _publishEndpoint.Publish<StockAdjustedEvent>(new
            {
                ProductId = adjustment.ProductId,
                QuantityChange = qtyChange,
                ReasonCode = adjustment.ReasonCode
            });

            _logger.LogInformation($"Dispatched StockAdjustedEvent for Product {adjustment.ProductId}");
            return true;
        }

        public async Task<IEnumerable<InventoryItemDto>> GetInventorySummaryAsync()
        {
            var adjustments = await _adjustmentRepository.GetQueryable()
                .AsNoTracking()
                .GroupBy(a => a.ProductId)
                .Select(g => new InventoryItemDto
                {
                    ProductId = g.Key,
                    StockQuantity = g.Sum(a => a.Quantity),
                    Name = $"Product {g.Key}", // Place-holder until product names are synced
                    SKU = $"SKU-{g.Key:D4}"
                })
                .ToListAsync();

            return adjustments;
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
