using Microsoft.EntityFrameworkCore;
using OrdersService.Models;
using OrdersService.Interfaces;

namespace OrdersService.Data
{
    public class OrdersDbContext : DbContext
    {
        private readonly ITenantProvider _tenantProvider;

        public OrdersDbContext(DbContextOptions<OrdersDbContext> options, ITenantProvider tenantProvider) : base(options) 
        {
            _tenantProvider = tenantProvider;
        }

        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillItem> BillItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Return> Returns { get; set; }
        public DbSet<Customer> Customers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Bill>()
                .HasIndex(b => b.BillNumber)
                .IsUnique();

            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.Bill)
                .WithMany(b => b.Items)
                .HasForeignKey(bi => bi.BillId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Bill)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BillId)
                .OnDelete(DeleteBehavior.Cascade);

            // Multi-tenant Global Query Filters
            modelBuilder.Entity<Bill>().HasQueryFilter(b => b.StoreId == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || b.StoreId == 0);
            modelBuilder.Entity<Customer>().HasQueryFilter(c => c.StoreId == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || c.StoreId == 0);
            modelBuilder.Entity<BillItem>().HasQueryFilter(bi => bi.StoreId == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || bi.StoreId == 0);
            modelBuilder.Entity<Payment>().HasQueryFilter(p => p.StoreId == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || p.StoreId == 0);
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

                    if (entry.Entity is Bill b) b.StoreId = (isAdmin && b.StoreId != 0) ? b.StoreId : currentStoreId;
                    if (entry.Entity is Customer c) c.StoreId = (isAdmin && c.StoreId != 0) ? c.StoreId : currentStoreId;
                    if (entry.Entity is BillItem bi) bi.StoreId = (isAdmin && bi.StoreId != 0) ? bi.StoreId : currentStoreId;
                    if (entry.Entity is Payment p) p.StoreId = (isAdmin && p.StoreId != 0) ? p.StoreId : currentStoreId;
                    if (entry.Entity is Return r) r.StoreId = (isAdmin && r.StoreId != 0) ? r.StoreId : currentStoreId;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
