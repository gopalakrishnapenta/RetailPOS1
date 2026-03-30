using System.ComponentModel.DataAnnotations;

namespace IdentityService.Models
{
    public class Store
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string StoreCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public virtual ICollection<UserStoreRole> UserRoles { get; set; } = new List<UserStoreRole>();
    }
}
