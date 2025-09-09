using KeremProject1backend.Models.DBs;

namespace KeremProject1backend.Models.Responses
{
    public class LoginResponse
    {
        public string Token { get; set; } = null!;
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserImageLink { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public UserRoleinAuthorization UserRoleinAuthorization { get; set; }
    }

    public class SignUpResponse
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? UserRole { get; set; }
        public string? Message { get; set; }
    }
} 