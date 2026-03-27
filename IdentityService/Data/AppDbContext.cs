using Microsoft.EntityFrameworkCore;
using IdentityService.Models;

namespace IdentityService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Store> Stores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Store>()
                .HasIndex(s => s.StoreCode)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.PrimaryStore)
                .WithMany(s => s.Users)
                .HasForeignKey(u => u.PrimaryStoreId)
                .OnDelete(DeleteBehavior.SetNull);

            // Data Seeding via Migrations
            modelBuilder.Entity<Store>().HasData(
                new Store { Id = 1, StoreCode = "S001", Name = "Nexus Main Store", Location = "Downtown", IsActive = true }
            );

            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Email = "admin@nexus.com", PasswordHash = "Admin@123", Role = "Admin", EmployeeCode = "E001", PrimaryStoreId = 1 },
                new User { Id = 2, Email = "admin@gmail.com", PasswordHash = "admin", Role = "Admin", EmployeeCode = "E003", PrimaryStoreId = 1 },
                new User { Id = 3, Email = "manager@nexus.com", PasswordHash = "Manager@123", Role = "Manager", EmployeeCode = "E004", PrimaryStoreId = 1 },
                new User { Id = 4, Email = "storemanager@nexus.com", PasswordHash = "SManager@123", Role = "StoreManager", EmployeeCode = "E005", PrimaryStoreId = 1 },
                new User { Id = 5, Email = "cashier@nexus.com", PasswordHash = "Cashier@123", Role = "Cashier", EmployeeCode = "E002", PrimaryStoreId = 1 }
            );
        }
    }
}
