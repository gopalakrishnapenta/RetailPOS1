using Microsoft.EntityFrameworkCore;
using CatalogService.Models;
using CatalogService.Interfaces;
using MassTransit;

namespace CatalogService.Data
{
    public class CatalogDbContext : DbContext
    {
        private readonly ITenantProvider _tenantProvider;

        public CatalogDbContext(DbContextOptions<CatalogDbContext> options, ITenantProvider tenantProvider) : base(options) 
        {
            _tenantProvider = tenantProvider;
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── MassTransit Outbox ───────────────────────────────────────────────────
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Sku)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Barcode);

            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.StoreId, p.IsActive });

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Name);

            modelBuilder.Entity<Category>()
                .HasIndex(c => new { c.StoreId, c.IsActive });

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Multi-tenant Global Query Filters ────────────────────────────────────
            modelBuilder.Entity<Product>().HasQueryFilter(p =>
                (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0)
                || p.StoreId == _tenantProvider.StoreId);

            modelBuilder.Entity<Category>().HasQueryFilter(c => 
                c.IsActive && (
                (_tenantProvider.Role == "Admin" && _tenantProvider.StoreId == 0)
                || c.StoreId == _tenantProvider.StoreId
                || c.StoreId == 0));

            modelBuilder.Entity<Category>()
                .Property(c => c.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Electronics", Description = "Gadgets", StoreId = 0 },
                new Category { Id = 2, Name = "Grocery", Description = "Daily essentials", StoreId = 0 },
                new Category { Id = 3, Name = "Beverages", Description = "Drinks", StoreId = 0 },
                new Category { Id = 4, Name = "Fruits", Description = "Fresh fruits", StoreId = 0 },
                new Category { Id = 5, Name = "Veggies", Description = "Fresh vegetables", StoreId = 0 }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Sku = "SKU-S1-001", Name = "Laptop (S1)", CategoryId = 1, MRP = 50000, SellingPrice = 45000, StoreId = 1, StockQuantity = 10 },
                new Product { Id = 2, Sku = "SKU-S1-002", Name = "Mechanical Keyboard (S1)", CategoryId = 1, MRP = 3000, SellingPrice = 2500, StoreId = 1, StockQuantity = 50 },
                new Product { Id = 3, Sku = "SKU-S2-003", Name = "Mouse (S2)", CategoryId = 1, MRP = 1000, SellingPrice = 800, StoreId = 2, StockQuantity = 100 },
                new Product { Id = 4, Sku = "SKU-S2-004", Name = "Milk (S2)", CategoryId = 2, MRP = 50, SellingPrice = 48, StoreId = 2, StockQuantity = 500 },
                new Product { Id = 5, Sku = "SKU-S2-005", Name = "Bread (S2)", CategoryId = 2, MRP = 40, SellingPrice = 38, StoreId = 2, StockQuantity = 200 }
            );
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity is Product p && p.StoreId == 0 && _tenantProvider != null) {
                        p.StoreId = _tenantProvider.StoreId;
                    }
                    if (entry.Entity is Category c && c.StoreId == 0 && _tenantProvider != null) {
                        c.StoreId = (_tenantProvider.Role == "Admin") ? 0 : _tenantProvider.StoreId;
                    }
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
