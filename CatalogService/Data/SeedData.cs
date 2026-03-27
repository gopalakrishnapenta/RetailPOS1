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
            // MigrateAsync handles DB creation and table generation from migrations
            await context.Database.MigrateAsync();
        }
    }
}
