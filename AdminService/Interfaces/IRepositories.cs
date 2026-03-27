using AdminService.Models;

namespace AdminService.Interfaces
{
    public interface IInventoryAdjustmentRepository : IGenericRepository<InventoryAdjustment>
    {
    }

    public interface IStoreRepository : IGenericRepository<Store>
    {
    }
}
