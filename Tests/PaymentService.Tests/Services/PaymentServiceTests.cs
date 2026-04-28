using NUnit.Framework;
using Moq;
using PaymentService.Services;
using PaymentService.Interfaces;
using PaymentService.Models;
using MassTransit;
using RetailPOS.Contracts;
using System.Threading.Tasks;

namespace PaymentService.Tests.Services
{
    [TestFixture]
    public class PaymentServiceTests
    {
        private Mock<IPaymentRepository> _paymentRepoMock;
        private Mock<IPublishEndpoint> _publishEndpointMock;
        private PaymentService.Services.PaymentService _paymentService;

        [SetUp]
        public void Setup()
        {
            _paymentRepoMock = new Mock<IPaymentRepository>();
            _publishEndpointMock = new Mock<IPublishEndpoint>();

            _paymentService = new PaymentService.Services.PaymentService(
                _paymentRepoMock.Object,
                _publishEndpointMock.Object
            );
        }

        [Test]
        public async Task ProcessPaymentAsync_ValidPayment_SavesAndPublishesEvent()
        {
            // Arrange
            var payment = new Payment { Id = 1, BillId = 100, Amount = 50.0m, PaymentMode = "CreditCard" };
            
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);
            _paymentRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _publishEndpointMock.Setup(p => p.Publish(It.IsAny<PaymentProcessedEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await _paymentService.ProcessPaymentAsync(payment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(1));
            
            _paymentRepoMock.Verify(r => r.AddAsync(payment), Times.Once);
            _paymentRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
