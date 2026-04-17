using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using CatalogService.Data;
using CatalogService.Interfaces;

namespace CatalogService.Data
{
    public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
    {
        public CatalogDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var builder = new DbContextOptionsBuilder<CatalogDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            builder.UseSqlServer(connectionString);

            return new CatalogDbContext(builder.Options, new DesignTimeTenantProvider());
        }
    }

    public class DesignTimeTenantProvider : ITenantProvider
    {
        public int StoreId => 0;
        public int UserId => 0;
        public string Role => "Admin"; // Allow migrations to see everything
        public string? Token => null;
    }
}
