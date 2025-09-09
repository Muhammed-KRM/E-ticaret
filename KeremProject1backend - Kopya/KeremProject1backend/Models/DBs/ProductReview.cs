using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeremProject1backend.Models.DBs
{
    [Table("ProductReviews")]
    public class ProductReview
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        [Required]
        public int UserId { get; set; } // Yorumu yapan kullanıcının ID'si
        // public virtual User User { get; set; } // User ilişkisi gerekirse

        // Yeni eklenen alan:
        [StringLength(100)] // Kullanıcı adını da saklayalım (denormalizasyon)
        public string? UserName { get; set; }

        [Required]
        [Range(1, 5)] // 1-5 arası puanlama
        public byte Rating { get; set; }

        public string? Comment { get; set; } // Yorum metni (isteğe bağlı olabilir)

        [Required]
        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

        public bool IsApproved { get; set; } = true; // Yönetici onayı için (varsayılan onaylı)

        // Navigation Properties
        // public virtual Product Product { get; set; }
        // public virtual AllUsersModel User { get; set; }
    }
} 