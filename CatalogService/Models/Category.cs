using System.ComponentModel.DataAnnotations;

namespace CatalogService.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int StoreId { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
