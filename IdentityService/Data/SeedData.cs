using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using IdentityService.Models;
using System.Linq;

namespace IdentityService.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = new AppDbContext(serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());
            Console.WriteLine("Applying Migrations and Seeding Identity Database...");
            
            // MigrateAsync handles DB creation and table generation from migrations
            await context.Database.MigrateAsync();
            
            // Database is now seeded via EF Core Migrations
            await context.Database.MigrateAsync();
        }
    }
}
