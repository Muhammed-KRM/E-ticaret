using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace KeremProject1backend.Models.DBs
{
    [Table("Files")]
    public class FileModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [Required]
        public long FileSize { get; set; }

        // Fiziksel depolama yolu veya blob referansı
        [Required]
        [StringLength(1000)]
        public string FilePath { get; set; } = string.Empty;

        // Dosyanın yüklendiği zaman
        public DateTime UploadDate { get; set; } = DateTime.Now;

        // Upload eden kullanıcı bilgisi (varsa)
        public int? UploadedByUserId { get; set; }
        
        // Dosya tipi (profil resmi, ürün resmi, belge vb.)
        [StringLength(50)]
        public string FileType { get; set; } = string.Empty;
        
        // İlişkili entity ID (örneğin: bu dosya hangi ürüne ait)
        public int? RelatedEntityId { get; set; }
        
        // Dosya durumu (active, deleted vb.)
        public bool IsActive { get; set; } = true;
        
        // Dosya açıklaması
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }
} 