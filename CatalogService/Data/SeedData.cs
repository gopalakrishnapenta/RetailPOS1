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

                // Check if categories exist
                if (!context.Categories.Any())
                {
                    logger.LogInformation("Seeding default categories...");
                    var categories = new List<Category>
                    {
                        new Category { Name = "Electronics", Description = "GADGETS & TECH", StoreId = 0, IsActive = true },
                        new Category { Name = "Groceries", Description = "DAILY ESSENTIALS", StoreId = 0, IsActive = true },
                        new Category { Name = "Apparel", Description = "CLOTHING & FASHION", StoreId = 0, IsActive = true }
                    };
                    context.Categories.AddRange(categories);
                    await context.SaveChangesAsync();
                }

                // Check if products exist
                if (!context.Products.Any())
                {
                    logger.LogInformation("Seeding default products...");
                    var electronics = await context.Categories.FirstAsync(c => c.Name == "Electronics");
                    var groceries = await context.Categories.FirstAsync(c => c.Name == "Groceries");

                    var products = new List<Product>
                    {
                        new Product { Name = "Wireless Mouse", Sku = "MOU-001", CategoryId = electronics.Id, MRP = 1500, SellingPrice = 999, StockQuantity = 100, StoreId = 0, IsActive = true },
                        new Product { Name = "Mechanical Keyboard", Sku = "KBD-001", CategoryId = electronics.Id, MRP = 4500, SellingPrice = 3200, StockQuantity = 50, StoreId = 0, IsActive = true },
                        new Product { Name = "Fresh Milk 1L", Sku = "MLK-001", CategoryId = groceries.Id, MRP = 65, SellingPrice = 60, StockQuantity = 500, StoreId = 0, IsActive = true },
                        new Product { Name = "Bread 400g", Sku = "BRD-001", CategoryId = groceries.Id, MRP = 45, SellingPrice = 40, StockQuantity = 200, StoreId = 0, IsActive = true }
                    };
                    context.Products.AddRange(products);
                    await context.SaveChangesAsync();
                }
                logger.LogInformation("Catalog Seeding Complete.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during Catalog seeding.");
            }
        }
    }
}
