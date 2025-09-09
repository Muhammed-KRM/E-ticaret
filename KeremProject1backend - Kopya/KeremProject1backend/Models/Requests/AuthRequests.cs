using KeremProject1backend.Models.DBs;
using System.ComponentModel.DataAnnotations; // For Required attribute

namespace KeremProject1backend.Models.Requests
{
    public class AuthRequests
    {

    }
    public class GetAllConfigurationDataRequest
    {

    }
    public class LoginRequest
    {
        [Required]
        public string UserName { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
    }

    public class SignUpRequest
    {
        [Required]
        public string UserName { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
        public string UserImageLink { get; set; } = string.Empty;
        
        // E-ticaret için gerekli bilgiler
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
    }

    public class RegisterAdminRequest
    {
        [Required]
        public string Username { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
        public string? UserImageLink { get; set; }
        
        // Admin için de iletişim bilgileri
        [EmailAddress]
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class UpdatePasswordRequest
    {
        [Required]
        public string OldPassword { get; set; } = null!;
        [Required]
        public string NewPassword { get; set; } = null!;
    }
}
