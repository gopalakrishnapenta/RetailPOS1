using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AdminService.Data;
using AdminService.Models;

var serviceCollection = new ServiceCollection();
serviceCollection.AddDbContext<AdminDbContext>(options =>
    options.UseSqlServer("Server=localhost;Database=RetailPOS_Admin;Trusted_Connection=True;TrustServerCertificate=True;"));

using var serviceProvider = serviceCollection.BuildServiceProvider();
using var scope = serviceProvider.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

var missingReturns = new[]
{
    new SyncedReturn { OrderId = 52, ReturnId = 10, StoreId = 1, RefundAmount = 375.00m, Date = DateTime.Parse("2026-04-12 08:28 AM") },
    new SyncedReturn { OrderId = 51, ReturnId = 9, StoreId = 1, RefundAmount = 8.00m, Date = DateTime.Parse("2026-04-12 08:28 AM") },
    new SyncedReturn { OrderId = 51, ReturnId = 8, StoreId = 1, RefundAmount = 16.00m, Date = DateTime.Parse("2026-04-12 08:07 AM") },
    new SyncedReturn { OrderId = 50, ReturnId = 7, StoreId = 1, RefundAmount = 750.00m, Date = DateTime.Parse("2026-04-12 07:48 AM") }
};

foreach (var r in missingReturns)
{
    if (!db.SyncedReturns.IgnoreQueryFilters().Any(existing => existing.ReturnId == r.ReturnId))
    {
        db.SyncedReturns.Add(r);
        Console.WriteLine($"Added Return {r.ReturnId} for Order {r.OrderId}");
    }
}

db.SaveChanges();
Console.WriteLine("Data Repair Complete.");
