using System.ComponentModel.DataAnnotations;

namespace OrdersService.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(20)]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Mobile number must be exactly 10 digits.")]
        public string? Mobile { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int StoreId { get; set; }
    }
}
