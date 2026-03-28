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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Multi-tenant Global Query Filter
            modelBuilder.Entity<InventoryAdjustment>().HasQueryFilter(a => a.StoreId == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0));
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<InventoryAdjustment>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.StoreId == 0)
                    {
                        entry.Entity.StoreId = _tenantProvider.StoreId;
                    }
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
