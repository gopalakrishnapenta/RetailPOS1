using Microsoft.EntityFrameworkCore;
using AdminService.Models;

namespace AdminService.Data
{
    public static class DbInitializer
    {
        public static async Task InitAsync(AdminDbContext context, ILogger logger)
        {
            logger.LogInformation("🚀 [AdminSync] Starting Store and Category Synchronization...");

            // 1. Seed DEFAULT STORES (Must match Identity Service exactly)
            var defaultStores = new List<AdminStoreEntity>
            {
                new AdminStoreEntity { Id = 1, StoreCode = "AP-VSKP-01", Name = "Vizag City", Location = "vizag", IsActive = true },
                new AdminStoreEntity { Id = 2, StoreCode = "AP-VJW-01", Name = "Vijayawada Central", Location = "Vijayawada", IsActive = true },
                new AdminStoreEntity { Id = 3, StoreCode = "AP-GTR-01", Name = "Guntur Regional11", Location = "Guntur", IsActive = true }
            };

            foreach (var store in defaultStores)
            {
                if (!await context.Stores.AnyAsync(s => s.Id == store.Id))
                {
                    logger.LogInformation($"[AdminSync] Seeding DEFAULT Store: {store.StoreCode} (ID: {store.Id})");
                    
                    // We use IDENTITY_INSERT for seeded IDs to match cross-service
                    await context.Database.OpenConnectionAsync();
                    try 
                    {
                        await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Stores ON");
                        context.Stores.Add(store);
                        await context.SaveChangesAsync();
                        await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Stores OFF");
                    }
                    finally 
                    {
                        await context.Database.CloseConnectionAsync();
                    }
                }
            }

            logger.LogInformation("✅ [AdminSync] Store Synchronization Complete.");
        }
    }
}
