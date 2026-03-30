using System.ComponentModel.DataAnnotations;

namespace IdentityService.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public ICollection<UserStoreRole> UserRoles { get; set; } = new List<UserStoreRole>();
    }
}
