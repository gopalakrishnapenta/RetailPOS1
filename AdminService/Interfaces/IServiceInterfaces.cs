using AdminService.DTOs;

namespace AdminService.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardAsync(int? storeId = null);
    }

    public interface IInventoryService
    {
        Task<bool> AdjustInventoryAsync(InventoryAdjustmentDto adjustmentDto);
        Task<PaginatedResult<InventoryAdjustmentDto>> GetAdjustmentsAsync(int page = 1, int pageSize = 5);
        Task<IEnumerable<InventoryItemDto>> GetInventorySummaryAsync();
    }

    public interface IReportService
    {
        Task<IEnumerable<SalesReportDto>> GetSalesReportAsync(DateTime? from, DateTime? to);
        Task<TaxReportDto> GetTaxReportAsync(DateTime? from, DateTime? to);
    }
}
