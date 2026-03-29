namespace IdentityService.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public string? EmployeeCode { get; set; }
        public int? PrimaryStoreId { get; set; }

        public Store PrimaryStore { get; set; }

        public string? Otp { get; set; }
        public DateTime? OtpExpiry { get; set; }

        public bool IsEmailVerified { get; set; } = false;
        public string? VerificationOtp { get; set; }
        public DateTime? VerificationOtpExpiry { get; set; }
    }
}
