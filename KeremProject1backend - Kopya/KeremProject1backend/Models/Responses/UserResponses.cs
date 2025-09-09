using KeremProject1backend.Models.DBs;

namespace KeremProject1backend.Models.Responses
{
    public class UserResponses
    {
    }
    public class UpdateUserResponses
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public UserRoleinAuthorization UserRoleinAuthorization { get; set; }
        public string? PasswordHash { get; set; }
        public bool State { get; set; }
        public string? UserImageLink { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public DateTime ModTime { get; set; }
        public int ModUser { get; set; }

    }

    public class UpdateMyProfileResponse
    {
        public int Id { get; set; }
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
        public DateTime? ModTime { get; set; }
    }
}
