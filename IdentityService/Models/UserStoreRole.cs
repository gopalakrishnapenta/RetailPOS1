using System.ComponentModel.DataAnnotations;

namespace IdentityService.Models
{
    public class UserStoreRole
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int? StoreId { get; set; } // Null for Global Admin
        public Store? Store { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }
    }
}
