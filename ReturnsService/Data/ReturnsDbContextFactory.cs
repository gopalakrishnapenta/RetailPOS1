using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ReturnsService.Data
{
    public class ReturnsDbContextFactory : IDesignTimeDbContextFactory<ReturnsDbContext>
    {
        public ReturnsDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var builder = new DbContextOptionsBuilder<ReturnsDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            builder.UseSqlServer(connectionString);

            return new ReturnsDbContext(builder.Options, new DesignTimeTenantProvider());
        }
    }

    public class DesignTimeTenantProvider : Interfaces.ITenantProvider
    {
        public int StoreId => 0;
        public string Role => "Admin";
        public string? Token => null;
    }
}
