using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace OrdersService.Data
{
    public class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
    {
        public OrdersDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var builder = new DbContextOptionsBuilder<OrdersDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            builder.UseSqlServer(connectionString);

            return new OrdersDbContext(builder.Options, new DesignTimeTenantProvider());
        }
    }

    public class DesignTimeTenantProvider : Interfaces.ITenantProvider
    {
        public int StoreId => 0;
        public int UserId => 0;
        public string Role => "Admin";
        public string? Token => null;
    }
}
