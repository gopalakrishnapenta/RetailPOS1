using System.ComponentModel.DataAnnotations;

namespace AdminService.Models
{
    public class AdminCategoryEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int StoreId { get; set; } = 0; // Global for admins
    }
}
