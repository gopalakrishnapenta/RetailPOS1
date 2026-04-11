using Microsoft.EntityFrameworkCore;
using BCryptNet = BCrypt.Net.BCrypt;
using IdentityService.Models;
using RetailPOS.Common.Authorization;
using System.Reflection;

namespace IdentityService.Data
{
    public static class DbInitializer
    {
        public static async Task InitAsync(AppDbContext context, ILogger logger)
        {
            logger.LogInformation("🚀 [RBAC] Starting Dynamic Permission Synchronization...");

            // 1. Get all Permission Constants from RetailPOS.Common using Reflection
            var permissionCodes = GetPermissionCodes();
            logger.LogInformation($"[RBAC] Found {permissionCodes.Count} permission definitions in code.");

            // 2. Ensure all Permissions exist in the database
            foreach (var code in permissionCodes)
            {
                var existing = await context.Permissions.FirstOrDefaultAsync(p => p.Code == code);
                if (existing == null)
                {
                    logger.LogInformation($"[RBAC] Syncing NEW permission: {code}");
                    context.Permissions.Add(new Permission { Code = code, Description = $"Auto-synced: {code}" });
                }
            }
            await context.SaveChangesAsync();

            // 3. Ensure Roles exist
            var roles = new[] { 
                (Id: 1, Name: "Admin"), 
                (Id: 2, Name: "StoreManager"), 
                (Id: 4, Name: "Cashier") 
            };

            foreach (var r in roles)
            {
                if (!await context.Roles.AnyAsync(role => role.Name == r.Name))
                {
                    context.Roles.Add(new Role { Name = r.Name });
                }
            }
            await context.SaveChangesAsync();

            // 4. Role-Permission Mappings
            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
            var managerRole = await context.Roles.FirstAsync(r => r.Name == "StoreManager");
            var cashierRole = await context.Roles.FirstAsync(r => r.Name == "Cashier");

            // Mapping: Admin
            await EnsureMapping(context, adminRole.Id, Permissions.System.All);

            // Mapping: Manager
            foreach (var code in permissionCodes.Where(c => c != Permissions.System.All && c != Permissions.Catalog.CategoriesEdit))
            {
                 await EnsureMapping(context, managerRole.Id, code);
            }

            // Mapping: Cashier
            var cashierSubset = new[] {
                Permissions.Orders.Create, Permissions.Orders.View, Permissions.Orders.ViewAll, Permissions.Orders.Finalize,
                Permissions.Orders.Hold, Permissions.Returns.Initiate, Permissions.Returns.View,
                Permissions.Catalog.View, Permissions.Catalog.CategoriesView,
                Permissions.Auth.Logout, Permissions.Auth.Refresh,
                Permissions.Payments.CreateOrder, Permissions.Payments.Verify
            };
            foreach (var code in cashierSubset)
            {
                await EnsureMapping(context, cashierRole.Id, code);
            }

            await context.SaveChangesAsync();
            logger.LogInformation("✅ [RBAC] Permission Sync Complete.");

            // 5. Ensure DEFAULT ADMIN user exists
            var adminEmail = "admin@gmail.com";
            var adminUser = await context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Email == adminEmail);
            if (adminUser == null)
            {
                logger.LogInformation($"[RBAC] Seeding DEFAULT ADMIN: {adminEmail}");
                adminUser = new User
                {
                    Email = adminEmail,
                    PasswordHash = BCryptNet.HashPassword("Admin@123"),
                    IsEmailVerified = true,
                    EmployeeCode = "ADM001"
                };
                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
            }

            if (!adminUser.UserRoles.Any())
            {
                logger.LogInformation($"[RBAC] Granting Global Admin to: {adminEmail}");
                adminUser.UserRoles.Add(new UserStoreRole
                {
                    RoleId = adminRole.Id,
                    StoreId = null 
                });
                await context.SaveChangesAsync();
            }

            // 6. Repair
            var managerRepair = new[] { 
                Permissions.Catalog.Manage, 
                Permissions.Catalog.View, 
                Permissions.Orders.View,
                Permissions.Notifications.View
            };
            foreach (var code in managerRepair)
            {
                await EnsureMapping(context, managerRole.Id, code);
            }

            var cashierRepair = new[] {
                Permissions.Catalog.View,
                Permissions.Catalog.CategoriesView,
                Permissions.Payments.CreateOrder,
                Permissions.Payments.Verify,
                Permissions.Orders.Create,
                Permissions.Orders.View,
                Permissions.Orders.ViewAll,
                Permissions.Orders.Finalize
            };
            foreach (var code in cashierRepair)
            {
                await EnsureMapping(context, cashierRole.Id, code);
            }
            await context.SaveChangesAsync();
        }

        private static async Task EnsureMapping(AppDbContext context, int roleId, string permissionCode)
        {
            var perm = await context.Permissions.FirstAsync(p => p.Code == permissionCode);
            if (!await context.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == perm.Id))
            {
                context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = perm.Id });
            }
        }

        private static List<string> GetPermissionCodes()
        {
            var codes = new List<string>();
            var type = typeof(Permissions);
            var nestedTypes = type.GetNestedTypes(BindingFlags.Public | BindingFlags.Static);
            foreach (var nested in nestedTypes)
            {
                var fields = nested.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                                   .Where(f => f.IsLiteral && !f.IsInitOnly);
                foreach (var field in fields)
                {
                    var val = field.GetRawConstantValue()?.ToString();
                    if (val != null) codes.Add(val);
                }
            }
            return codes;
        }
    }
}
