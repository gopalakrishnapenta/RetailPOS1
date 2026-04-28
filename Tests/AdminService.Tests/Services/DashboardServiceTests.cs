using NUnit.Framework;
using Moq;
using AdminService.Services;
using AdminService.Interfaces;
using AdminService.Models;
using AdminService.DTOs;
using AdminService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdminService.Tests.Services
{
    [TestFixture]
    public class DashboardServiceTests
    {
        private AdminDbContext _dbContext;
        private Mock<ILogger<DashboardService>> _loggerMock;
        private DashboardService _dashboardService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AdminDbContext>()
                .UseInMemoryDatabase(databaseName: "AdminTestDb_" + Guid.NewGuid().ToString())
                .Options;

            // Assuming a basic mock tenant provider for context construction if required
            var tenantProviderMock = new Mock<ITenantProvider>();
            
            _dbContext = new AdminDbContext(options, tenantProviderMock.Object);
            _loggerMock = new Mock<ILogger<DashboardService>>();

            _dashboardService = new DashboardService(_dbContext, _loggerMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Test]
        public async Task GetDashboardAsync_CalculatesMetricsCorrectly()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;
            
            _dbContext.SyncedOrders.Add(new SyncedOrder { OrderId = 1, TotalAmount = 100, StoreId = 1, Date = today, CashierId = 1 });
            _dbContext.SyncedOrders.Add(new SyncedOrder { OrderId = 2, TotalAmount = 200, StoreId = 1, Date = today, CashierId = 2 });
            _dbContext.SyncedReturns.Add(new SyncedReturn { Id = 1, RefundAmount = 50, StoreId = 1, Date = today, OrderId = 1, ReturnId = 1 });
            
            _dbContext.StaffMembers.Add(new StaffMember { UserId = 1, Email = "john.doe@example.com", FullName = "John Doe" });
            _dbContext.StaffMembers.Add(new StaffMember { UserId = 2, Email = "jane.smith@example.com", FullName = "Jane Smith" });
            
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _dashboardService.GetDashboardAsync(storeId: 1);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TodaySales, Is.EqualTo(250));
            Assert.That(result.RefundedAmount, Is.EqualTo(50));
            
            // Checking Staff Performance (Leaderboard)
            Assert.That(result.StaffLeaderboard, Is.Not.Null);
            Assert.That(result.StaffLeaderboard.Count(), Is.GreaterThanOrEqualTo(2));
            var topStaff = result.StaffLeaderboard.OrderByDescending(s => s.Sales).First();
            Assert.That(topStaff.Sales, Is.EqualTo(200));
            Assert.That(topStaff.StaffName, Does.Contain("Jane"));
        }
    }
}
