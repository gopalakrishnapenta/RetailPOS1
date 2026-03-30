using Microsoft.EntityFrameworkCore;
using IdentityService.Models;

namespace IdentityService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserStoreRole> UserStoreRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Core Indexes
            modelBuilder.Entity<Store>().HasIndex(s => s.StoreCode).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Permission>().HasIndex(p => p.Code).IsUnique();
            
            // Soft-Delete Filter
            modelBuilder.Entity<Store>().HasQueryFilter(s => s.IsActive);

            // RolePermission Many-to-Many Configuration
            modelBuilder.Entity<RolePermission>().HasKey(rp => new { rp.RoleId, rp.PermissionId });
            modelBuilder.Entity<RolePermission>().HasOne(rp => rp.Role).WithMany(r => r.RolePermissions).HasForeignKey(rp => rp.RoleId);
            modelBuilder.Entity<RolePermission>().HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions).HasForeignKey(rp => rp.PermissionId);

            // UserStoreRole Configuration
            modelBuilder.Entity<UserStoreRole>().HasOne(usr => usr.User).WithMany(u => u.UserRoles).HasForeignKey(usr => usr.UserId);
            modelBuilder.Entity<UserStoreRole>().HasOne(usr => usr.Store).WithMany(s => s.UserRoles).HasForeignKey(usr => usr.StoreId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<UserStoreRole>().HasOne(usr => usr.Role).WithMany(r => r.UserRoles).HasForeignKey(usr => usr.RoleId);

            // --- DATA SEEDING (RBAC) ---
            // Note: Permissions and Role Mappings are now handled dynamically via DbInitializer.cs
            // to avoid migration bloat.
        }
    }
}
