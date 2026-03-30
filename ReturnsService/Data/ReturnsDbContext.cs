using Microsoft.EntityFrameworkCore;
using ReturnsService.Models;
using ReturnsService.Interfaces;

namespace ReturnsService.Data
{
    public class ReturnsDbContext : DbContext
    {
        private readonly ITenantProvider _tenantProvider;

        public ReturnsDbContext(DbContextOptions<ReturnsDbContext> options, ITenantProvider tenantProvider) : base(options) 
        {
            _tenantProvider = tenantProvider;
        }

        public DbSet<Return> Returns { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Multi-tenant Global Query Filters
            modelBuilder.Entity<Return>().HasQueryFilter(r => r.StoreId == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || r.StoreId == 0);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added)
                {
                    bool isAdmin = _tenantProvider.Role == "Admin";
                    int currentStoreId = _tenantProvider.StoreId;

                    if (entry.Entity is Return r) r.StoreId = (isAdmin && r.StoreId != 0) ? r.StoreId : currentStoreId;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
