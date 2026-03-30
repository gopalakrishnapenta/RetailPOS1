using System.ComponentModel.DataAnnotations;

namespace IdentityService.Models
{
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty; // e.g., "orders:create"

        [MaxLength(250)]
        public string Description { get; set; } = string.Empty;

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
