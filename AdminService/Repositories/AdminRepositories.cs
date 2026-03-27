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

    public class StoreRepository : GenericRepository<AdminStoreEntity>, IStoreRepository
    {
        public StoreRepository(AdminDbContext context) : base(context)
        {
        }
    }
}
