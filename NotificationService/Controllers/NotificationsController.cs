using Microsoft.AspNetCore.Mvc;
using NotificationService.Interfaces;
using NotificationService.Services;
using Microsoft.AspNetCore.Authorization;
using RetailPOS.Common.Authorization;

namespace NotificationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("{recipient}")]
        [Authorize]
        public async Task<IActionResult> GetRecent(string recipient)
        {
            // For security, only allow users to see their own notifications or Admins
            var results = await _notificationService.GetRecentNotificationsAsync(recipient);
            return Ok(results);
        }

        [HttpPost("test-signalr")]
        public async Task<IActionResult> TestSignalR([FromBody] string message)
        {
            await _notificationService.SendRealTimeNotificationAsync(message);
            return Ok(new { message = "SignalR message pushed" });
        }
    }
}
