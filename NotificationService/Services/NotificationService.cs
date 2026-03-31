using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;
using NotificationService.Models;
using NotificationService.Interfaces;

namespace NotificationService.Services
{
    public interface INotificationService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendSmsAsync(string to, string message);
        Task SendRealTimeNotificationAsync(string message, string? userId = null, int? storeId = null);
        Task<IEnumerable<Notification>> GetRecentNotificationsAsync(string recipient);
    }

    public class InternalNotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<InternalNotificationService> _logger;

        public InternalNotificationService(
            INotificationRepository repository, 
            IHubContext<NotificationHub> hubContext,
            ILogger<InternalNotificationService> logger)
        {
            _repository = repository;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            _logger.LogInformation($"[EMAIL] To: {to}, Subject: {subject}");
            
            var notification = new Notification
            {
                Type = "Email",
                Recipient = to,
                Content = $"{subject}: {body}",
                Status = "Sent" // Mock sent
            };

            await _repository.AddAsync(notification);
            await _repository.SaveChangesAsync();
        }

        public async Task SendSmsAsync(string to, string message)
        {
            _logger.LogInformation($"[SMS] To: {to}, Message: {message}");

            var notification = new Notification
            {
                Type = "Sms",
                Recipient = to,
                Content = message,
                Status = "Sent" // Mock sent
            };

            await _repository.AddAsync(notification);
            await _repository.SaveChangesAsync();
        }

        public async Task SendRealTimeNotificationAsync(string message, string? userId = null, int? storeId = null)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", "System", message);
            }
            else if (storeId.HasValue)
            {
                await _hubContext.Clients.Group($"Store_{storeId}").SendAsync("ReceiveNotification", "System", message);
            }
            else
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", "System", message);
            }

            var notification = new Notification
            {
                Type = "SignalR",
                Recipient = userId ?? (storeId.HasValue ? $"Store_{storeId}" : "All"),
                Content = message,
                Status = "Sent"
            };

            await _repository.AddAsync(notification);
            await _repository.SaveChangesAsync();
        }

        public async Task<IEnumerable<Notification>> GetRecentNotificationsAsync(string recipient)
        {
            return await _repository.GetRecentByRecipientAsync(recipient);
        }
    }
}
