using System.ComponentModel.DataAnnotations;

namespace IdentityService.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? EmployeeCode { get; set; }

        public string? Otp { get; set; }
        public DateTime? OtpExpiry { get; set; }

        public bool IsEmailVerified { get; set; } = false;
        public string? VerificationOtp { get; set; }
        public DateTime? VerificationOtpExpiry { get; set; }

        // New relationship for Multi-Tenant RBAC
        public virtual ICollection<UserStoreRole> UserRoles { get; set; } = new List<UserStoreRole>();
    }
}
