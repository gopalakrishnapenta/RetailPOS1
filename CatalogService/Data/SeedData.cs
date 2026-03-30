using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using CatalogService.Models;
using System.Linq;

namespace CatalogService.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = serviceProvider.GetRequiredService<CatalogDbContext>();
            var logger = serviceProvider.GetRequiredService<ILogger<CatalogDbContext>>();
            
            try 
            {
                logger.LogInformation("Attempting to apply Catalog migrations...");
                await context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning("Migration failed or already applied: {Message}. Proceeding to service startup.", ex.Message);
            }
        }
    }
}
