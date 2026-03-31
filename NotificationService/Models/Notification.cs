using System.ComponentModel.DataAnnotations;

namespace NotificationService.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Type { get; set; } = string.Empty; // Email, SMS, SignalR
        
        [Required]
        public string Recipient { get; set; } = string.Empty; // Email address, Phone number, or User ID
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        
        public bool IsRead { get; set; } = false;
        
        public string? Status { get; set; } // Pending, Sent, Failed
    }
}
