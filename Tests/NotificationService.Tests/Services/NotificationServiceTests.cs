using NUnit.Framework;
using Moq;
using NotificationService.Services;
using NotificationService.Interfaces;
using NotificationService.Models;
using NotificationService.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NotificationService.Tests.Services
{
    [TestFixture]
    public class NotificationServiceTests
    {
        private Mock<INotificationRepository> _repositoryMock;
        private Mock<IHubContext<NotificationHub>> _hubContextMock;
        private Mock<IClientProxy> _clientProxyMock;
        private Mock<IHubClients> _hubClientsMock;
        private Mock<ILogger<InternalNotificationService>> _loggerMock;
        private InternalNotificationService _notificationService;

        [SetUp]
        public void Setup()
        {
            _repositoryMock = new Mock<INotificationRepository>();
            _hubContextMock = new Mock<IHubContext<NotificationHub>>();
            _clientProxyMock = new Mock<IClientProxy>();
            _hubClientsMock = new Mock<IHubClients>();
            _loggerMock = new Mock<ILogger<InternalNotificationService>>();

            // Setup SignalR Hub mocks
            _hubClientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);
            _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);

            _notificationService = new InternalNotificationService(
                _repositoryMock.Object,
                _hubContextMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task SendEmailAsync_SavesNotificationAndSendsViaHub()
        {
            // Arrange
            string to = "test@example.com";
            string subject = "Test Subject";
            string body = "Test Body";

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);
            _repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _notificationService.SendEmailAsync(to, subject, body);

            // Assert
            _repositoryMock.Verify(r => r.AddAsync(It.Is<Notification>(n => 
                n.Type == "Email" && 
                n.Recipient == to && 
                n.Content.Contains(subject) && 
                n.Content.Contains(body)
            )), Times.Once);
            
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
