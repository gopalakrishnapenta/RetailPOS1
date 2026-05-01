using Microsoft.EntityFrameworkCore;
using AdminService.Models;
using AdminService.Interfaces;
using MassTransit;

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
        public DbSet<SyncedReturn> SyncedReturns { get; set; }
        public DbSet<SyncedProduct> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── MassTransit Outbox ───────────────────────────────────────────────────
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

            // ── Multi-tenant Global Query Filters ────────────────────────────────────
            modelBuilder.Entity<InventoryAdjustment>().HasQueryFilter(a => a.StoreId == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || a.StoreId == 0);
            modelBuilder.Entity<AdminStoreEntity>().HasQueryFilter(s => s.IsActive && (s.Id == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || s.Id == 0));
            modelBuilder.Entity<AdminCategoryEntity>().HasQueryFilter(c => c.IsActive && (c.StoreId == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || c.StoreId == 0));
            modelBuilder.Entity<SyncedOrder>().HasQueryFilter(o => o.StoreId == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || o.StoreId == 0);
            modelBuilder.Entity<SyncedReturn>().HasQueryFilter(r => r.StoreId == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || r.StoreId == 0);
            modelBuilder.Entity<SyncedProduct>().HasQueryFilter(p => p.StoreId == _tenantProvider.StoreId || (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || p.StoreId == 0);
            
            // ── Performance Indexes ──────────────────────────────────────────────────
            modelBuilder.Entity<SyncedOrder>()
                .HasIndex(o => new { o.StoreId, o.Date });

            modelBuilder.Entity<InventoryAdjustment>()
                .HasIndex(a => a.ProductId);

            modelBuilder.Entity<StaffMember>()
                .HasIndex(s => s.Email)
                .IsUnique();
            // Disable identity generation for the synced key (we use the ID from OrdersService)
            modelBuilder.Entity<SyncedOrder>(entity => {
                entity.Property(o => o.OrderId).ValueGeneratedNever();
                entity.Property(o => o.TaxAmount).HasPrecision(18, 2);
                entity.Property(o => o.TotalAmount).HasPrecision(18, 2);
                entity.Property(o => o.CashierId).HasDefaultValue(0);
            });

            modelBuilder.Entity<DashboardStats>(entity => {
                entity.Property(d => d.TodaySales).HasPrecision(18, 2);
                entity.Property(d => d.TotalSales).HasPrecision(18, 2);
            });

            modelBuilder.Entity<SyncedReturn>(entity => {
                entity.Property(r => r.RefundAmount).HasPrecision(18, 2);
            });

            modelBuilder.Entity<SyncedProduct>(entity => {
                entity.Property(p => p.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<StaffMember>().HasQueryFilter(sm => 
                sm.AssignedStoreId == _tenantProvider.StoreId || 
                (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0) || 
                sm.AssignedStoreId == 0 || 
                sm.AssignedStoreId == null);
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
