using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeremProject1backend.Models.DBs
{
    public class UsersModels
    {
    }

    [Table("Users")]
    public class AllUsersModels
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(64)]
        public string UserName { get; set; } = null!;
        [Required]
        public UserRoleinAuthorization UserRoleinAuthorization { get; set; }
        [Required]
        [StringLength(512)]
        public string PasswordHash { get; set; } = null!;
        [Required]
        public bool? State { get; set; }
        [StringLength(1024)]
        public string? UserImageLink { get; set; }
        [StringLength(256)]
        public string? Email { get; set; }
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        // E-ticaret için adres bilgileri
        [StringLength(500)]
        public string? Address { get; set; }
        [StringLength(100)]
        public string? City { get; set; }
        [StringLength(100)]
        public string? District { get; set; }
        [StringLength(10)]
        public string? PostalCode { get; set; }
        [StringLength(100)]
        public string? Country { get; set; }
        
        public DateTime? ModTime { get; set; }
        public int? ModUser { get; set; }

    }

}
