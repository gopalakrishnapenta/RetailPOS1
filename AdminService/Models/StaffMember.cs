using System.ComponentModel.DataAnnotations;

namespace AdminService.Models
{
    public class StaffMember
    {
        [Key]
        public int UserId { get; set; } // Matches the Id from Identity Service
        
        [Required]
        public string Email { get; set; }
        
        public string? FullName { get; set; }
        
        public bool IsAssigned { get; set; } = false;
        
        public int? AssignedStoreId { get; set; }
        
        public string? AssignedRole { get; set; }
        
        public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;
    }
}
