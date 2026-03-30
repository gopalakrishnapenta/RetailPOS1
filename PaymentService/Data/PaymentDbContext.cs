using Microsoft.EntityFrameworkCore;
using PaymentService.Models;
using PaymentService.Interfaces;

namespace PaymentService.Data
{
    public class PaymentDbContext : DbContext
    {
        private readonly ITenantProvider _tenantProvider;

        public PaymentDbContext(DbContextOptions<PaymentDbContext> options, ITenantProvider tenantProvider) : base(options) 
        {
            _tenantProvider = tenantProvider;
        }

        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Multi-tenant Global Query Filters
            modelBuilder.Entity<Payment>().HasQueryFilter(p => p.StoreId == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || p.StoreId == 0);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added)
                {
                    bool isAdmin = _tenantProvider.Role == "Admin";
                    int currentStoreId = _tenantProvider.StoreId;

                    if (entry.Entity is Payment p) p.StoreId = (isAdmin && p.StoreId != 0) ? p.StoreId : currentStoreId;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
