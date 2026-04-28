using NUnit.Framework;
using Moq;
using OrdersService.Services;
using OrdersService.Interfaces;
using OrdersService.Models;
using OrdersService.DTOs;
using OrdersService.Exceptions;
using MassTransit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrdersService.Tests.Services
{
    [TestFixture]
    public class BillServiceTests
    {
        private FakeBillRepository _fakeBillRepo;
        private Mock<IBillItemRepository> _billItemRepoMock;
        private Mock<ICustomerRepository> _customerRepoMock;
        private Mock<ITenantProvider> _tenantProviderMock;
        private Mock<IPublishEndpoint> _publishEndpointMock;
        
        private BillService _billService;

        [SetUp]
        public void Setup()
        {
            _fakeBillRepo = new FakeBillRepository();
            _billItemRepoMock = new Mock<IBillItemRepository>();
            _customerRepoMock = new Mock<ICustomerRepository>();
            _tenantProviderMock = new Mock<ITenantProvider>();
            _publishEndpointMock = new Mock<IPublishEndpoint>();

            _billService = new BillService(
                _fakeBillRepo,
                _billItemRepoMock.Object,
                _customerRepoMock.Object,
                _tenantProviderMock.Object,
                _publishEndpointMock.Object
            );
        }

        [Test]
        public async Task GetAllBillsAsync_ReturnsMappedDtos()
        {
            // Arrange
            _fakeBillRepo.Bills.Add(new Bill { Id = 1, BillNumber = "B-001", TotalAmount = 100, Status = "Finalized", Items = new List<BillItem>() });
            _fakeBillRepo.Bills.Add(new Bill { Id = 2, BillNumber = "B-002", TotalAmount = 200, Status = "Draft", Items = new List<BillItem>() });

            // Act
            var result = await _billService.GetAllBillsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.First().BillNumber, Is.EqualTo("B-001"));
        }

        [Test]
        public void GetBillByIdAsync_NotFound_ThrowsNotFoundException()
        {
            // Arrange
            _fakeBillRepo.Bills.Clear();

            // Act & Assert
            var ex = Assert.ThrowsAsync<NotFoundException>(() => _billService.GetBillByIdAsync(99));
            Assert.That(ex.Message, Does.Contain("not found"));
        }

        [Test]
        public async Task GetBillByIdAsync_Found_ReturnsDto()
        {
            // Arrange
            _fakeBillRepo.Bills.Add(new Bill { Id = 1, BillNumber = "B-001", TotalAmount = 100, Status = "Finalized", Items = new List<BillItem>() });

            // Act
            var result = await _billService.GetBillByIdAsync(1);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.BillNumber, Is.EqualTo("B-001"));
        }
    }

    public class FakeBillRepository : IBillRepository
    {
        public List<Bill> Bills { get; set; } = new List<Bill>();

        public Task<IEnumerable<Bill>> GetAllBillsWithItemsAsync()
        {
            return Task.FromResult<IEnumerable<Bill>>(Bills);
        }

        public Task<Bill?> GetBillWithItemsAsync(int id)
        {
            return Task.FromResult(Bills.FirstOrDefault(b => b.Id == id));
        }

        public Task<IEnumerable<Bill>> GetBillsByCustomerAsync(string mobile) => Task.FromResult<IEnumerable<Bill>>(Bills.Where(b => b.CustomerMobile == mobile));
        public Task<Bill?> GetByBillNumberAsync(string billNumber) => Task.FromResult(Bills.FirstOrDefault(b => b.BillNumber == billNumber));
        public Task<IEnumerable<Bill>> GetBillsByStatusAsync(string status) => Task.FromResult<IEnumerable<Bill>>(Bills.Where(b => b.Status == status));
        public Task<IEnumerable<Bill>> GetBillsByStoreAsync(int storeId) => Task.FromResult<IEnumerable<Bill>>(Bills.Where(b => b.StoreId == storeId));

        public Task<Bill?> GetByIdAsync(int id) => Task.FromResult(Bills.FirstOrDefault(b => b.Id == id));
        public Task<IEnumerable<Bill>> GetAllAsync() => Task.FromResult<IEnumerable<Bill>>(Bills);
        public Task<IEnumerable<Bill>> FindAsync(System.Linq.Expressions.Expression<System.Func<Bill, bool>> predicate) => Task.FromResult<IEnumerable<Bill>>(Bills.AsQueryable().Where(predicate));
        public Task<Bill?> SingleOrDefaultAsync(System.Linq.Expressions.Expression<System.Func<Bill, bool>> predicate) => Task.FromResult(Bills.AsQueryable().SingleOrDefault(predicate));
        public Task<bool> AnyAsync(System.Linq.Expressions.Expression<System.Func<Bill, bool>> predicate) => Task.FromResult(Bills.AsQueryable().Any(predicate));
        
        public Task AddAsync(Bill entity)
        {
            Bills.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(Bill entity) { }
        public void Delete(Bill entity) { Bills.Remove(entity); }
        public Task SaveChangesAsync() => Task.CompletedTask;
        public IQueryable<Bill> GetQueryable() => Bills.AsQueryable();
        public void DeleteRange(IEnumerable<Bill> entities) { foreach(var e in entities) Bills.Remove(e); }
    }
}
