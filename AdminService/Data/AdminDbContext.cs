using Microsoft.EntityFrameworkCore;
using AdminService.Models;
using AdminService.Interfaces;

namespace AdminService.Data
{
    public class AdminDbContext : DbContext
    {
        private readonly ITenantProvider _tenantProvider;

        public AdminDbContext(DbContextOptions<AdminDbContext> options, ITenantProvider tenantProvider) : base(options) 
        {
            _tenantProvider = tenantProvider;
        }

        public DbSet<InventoryAdjustment> InventoryAdjustments { get; set; }
        public DbSet<AdminStoreEntity> Stores { get; set; }
        public DbSet<AdminCategoryEntity> Categories { get; set; }
        public DbSet<SyncedOrder> SyncedOrders { get; set; }
        public DbSet<DashboardStats> DashboardStats { get; set; }
        public DbSet<StaffMember> StaffMembers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Multi-tenant Global Query Filters ────────────────────────────────────
            modelBuilder.Entity<InventoryAdjustment>().HasQueryFilter(a => a.StoreId == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || a.StoreId == 0);
            modelBuilder.Entity<AdminStoreEntity>().HasQueryFilter(s => s.IsActive && (s.Id == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || s.Id == 0));
            modelBuilder.Entity<AdminCategoryEntity>().HasQueryFilter(c => c.IsActive && (c.StoreId == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || c.StoreId == 0));
            modelBuilder.Entity<SyncedOrder>().HasQueryFilter(o => o.StoreId == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || o.StoreId == 0);
            modelBuilder.Entity<StaffMember>().HasQueryFilter(sm => sm.AssignedStoreId == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || sm.AssignedStoreId == 0);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added)
                {
                    bool isAdmin = _tenantProvider.Role == "Admin";
                    int currentStoreId = _tenantProvider.StoreId;

                    if (entry.Entity is InventoryAdjustment a && a.StoreId == 0) a.StoreId = currentStoreId;
                    if (entry.Entity is AdminCategoryEntity c && c.StoreId == 0) c.StoreId = (isAdmin) ? 0 : currentStoreId;
                    // Note: AdminStoreEntity usually handles its own ID as Primary Key, 
                    // but if multi-tenant Store Managers can create sub-stores, we'd handle it here.
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
