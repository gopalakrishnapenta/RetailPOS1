using System.ComponentModel.DataAnnotations;

namespace AdminService.Models
{
    public class Store
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string StoreCode { get; set; } = string.Empty;
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
