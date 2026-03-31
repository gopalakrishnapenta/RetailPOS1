using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdminService.Models
{
    public class StaffMember
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
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
