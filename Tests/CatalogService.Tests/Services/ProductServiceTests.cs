using NUnit.Framework;
using Moq;
using CatalogService.Services;
using CatalogService.Interfaces;
using CatalogService.Models;
using CatalogService.DTOs;
using CatalogService.Exceptions;
using CatalogService.Data;
using CatalogService.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CatalogService.Tests.Services
{
    [TestFixture]
    public class ProductServiceTests
    {
        private CatalogDbContext _dbContext;
        private ProductRepository _productRepo;
        private Mock<ICategoryRepository> _categoryRepoMock;
        private Mock<IPublishEndpoint> _publishEndpointMock;
        private Mock<ITenantProvider> _tenantProviderMock;
        private ProductService _productService;

        [SetUp]
        public void Setup()
        {
            _tenantProviderMock = new Mock<ITenantProvider>();
            _tenantProviderMock.Setup(t => t.StoreId).Returns(1);
            _tenantProviderMock.Setup(t => t.Role).Returns("Admin");

            var options = new DbContextOptionsBuilder<CatalogDbContext>()
                .UseInMemoryDatabase(databaseName: "CatalogTestDb_" + System.Guid.NewGuid().ToString())
                .Options;

            _dbContext = new CatalogDbContext(options, _tenantProviderMock.Object);
            _productRepo = new ProductRepository(_dbContext);
            
            _categoryRepoMock = new Mock<ICategoryRepository>();
            _publishEndpointMock = new Mock<IPublishEndpoint>();

            _productService = new ProductService(
                _productRepo,
                _tenantProviderMock.Object,
                _publishEndpointMock.Object
            );
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Test]
        public async Task GetAllProductsAsync_ReturnsActiveProducts()
        {
            // Arrange
            _dbContext.Categories.Add(new Category { Id = 1, Name = "Cat1", IsActive = true, StoreId = 1 });
            _dbContext.Products.Add(new Product { Id = 1, Name = "Prod1", Sku = "S1", IsActive = true, CategoryId = 1, StoreId = 1 });
            _dbContext.Products.Add(new Product { Id = 2, Name = "Prod2", Sku = "S2", IsActive = false, CategoryId = 1, StoreId = 1 });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _productService.GetAllProductsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().Name, Is.EqualTo("Prod1"));
        }

        [Test]
        public async Task CreateProductAsync_ValidProduct_SavesToDatabase()
        {
            // Arrange
            _dbContext.Categories.Add(new Category { Id = 1, Name = "Cat1", IsActive = true, StoreId = 1 });
            await _dbContext.SaveChangesAsync();

            var dto = new ProductDto
            {
                Name = "NewProd",
                Sku = "NP1",
                CategoryId = 1,
                SellingPrice = 100,
                MRP = 120,
                StockQuantity = 10,
                IsActive = true
            };
            
            _categoryRepoMock.Setup(c => c.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, Name = "Cat1" });

            // Act
            var result = await _productService.CreateProductAsync(dto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.GreaterThan(0));
            Assert.That(_dbContext.Products.Count(), Is.EqualTo(1));
        }

        [Test]
        public void CreateProductAsync_DuplicateSku_ThrowsValidationException()
        {
            // Arrange
            _dbContext.Categories.Add(new Category { Id = 1, Name = "Cat1", IsActive = true, StoreId = 1 });
            _dbContext.Products.Add(new Product { Id = 1, Name = "Existing", Sku = "NP1", IsActive = true, StoreId = 1, CategoryId = 1 });
            _dbContext.SaveChanges();

            var dto = new ProductDto
            {
                Name = "NewProd",
                Sku = "NP1",
                CategoryId = 1,
                SellingPrice = 100,
                MRP = 120,
                StockQuantity = 10,
                IsActive = true
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ConflictException>(() => _productService.CreateProductAsync(dto));
            Assert.That(ex.Message, Does.Contain("SKU"));
        }
    }
}
