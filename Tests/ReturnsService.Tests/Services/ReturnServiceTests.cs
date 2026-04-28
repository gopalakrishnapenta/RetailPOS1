using NUnit.Framework;
using Moq;
using ReturnsService.Services;
using ReturnsService.Interfaces;
using ReturnsService.Models;
using ReturnsService.DTOs;
using MassTransit;
using RetailPOS.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReturnsService.Tests.Services
{
    [TestFixture]
    public class ReturnServiceTests
    {
        private Mock<IReturnRepository> _returnRepoMock;
        private Mock<IPublishEndpoint> _publishEndpointMock;
        private Mock<ITenantProvider> _tenantProviderMock;
        private ReturnService _returnService;

        [SetUp]
        public void Setup()
        {
            _returnRepoMock = new Mock<IReturnRepository>();
            _publishEndpointMock = new Mock<IPublishEndpoint>();
            _tenantProviderMock = new Mock<ITenantProvider>();

            _tenantProviderMock.Setup(t => t.StoreId).Returns(1);

            _returnService = new ReturnService(
                _returnRepoMock.Object,
                _publishEndpointMock.Object,
                _tenantProviderMock.Object
            );
        }

        [Test]
        public async Task InitiateReturnAsync_ValidRequest_CreatesReturnsAndPublishesEvent()
        {
            // Arrange
            var returnDto = new ReturnInitiationDto
            {
                BillId = 100,
                Reason = "Defective",
                CustomerMobile = "1234567890",
                Items = new List<ReturnItemDto>
                {
                    new ReturnItemDto { BillItemId = 1, Quantity = 2, RefundAmount = 50.0m }
                }
            };

            _returnRepoMock.Setup(r => r.AddAsync(It.IsAny<Return>())).Returns(Task.CompletedTask);
            _returnRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _returnService.InitiateReturnAsync(returnDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.OriginalBillId, Is.EqualTo(100));
            Assert.That(result.Status, Is.EqualTo("Initiated"));
            
            _returnRepoMock.Verify(r => r.AddAsync(It.IsAny<Return>()), Times.Once);
            _returnRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
