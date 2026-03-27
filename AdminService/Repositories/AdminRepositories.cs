using AdminService.Data;
using AdminService.Models;
using AdminService.Interfaces;

namespace AdminService.Repositories
{
    public class InventoryAdjustmentRepository : GenericRepository<InventoryAdjustment>, IInventoryAdjustmentRepository
    {
        public InventoryAdjustmentRepository(AdminDbContext context) : base(context)
        {
        }
    }

    public class StoreRepository : GenericRepository<Store>, IStoreRepository
    {
        public StoreRepository(AdminDbContext context) : base(context)
        {
        }
    }
}
