using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KeremProject1backend.Models.Requests
{
    // Tekli dosya yükleme isteği
    public class FileUploadRequest
    {
        [Required(ErrorMessage = "Dosya zorunludur")]
        public IFormFile File { get; set; } = null!;
        
        // Dosya tipi (profil resmi, ürün resmi, belge vb.)
        [StringLength(50)]
        public string FileType { get; set; } = string.Empty;
        
        // İlişkili entity ID (örneğin: bu dosya hangi ürüne ait)
        public int? RelatedEntityId { get; set; }
        
        // Dosya açıklaması (opsiyonel)
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }
    
    // Çoklu dosya yükleme isteği
    public class MultipleFileUploadRequest
    {
        [Required(ErrorMessage = "En az bir dosya zorunludur")]
        public List<IFormFile> Files { get; set; } = new List<IFormFile>();
        
        // Dosya tipi (profil resmi, ürün resmi, belge vb.)
        [StringLength(50)]
        public string FileType { get; set; } = string.Empty;
        
        // İlişkili entity ID (örneğin: bu dosya hangi ürüne ait)
        public int? RelatedEntityId { get; set; }
        
        // Dosya açıklaması (opsiyonel)
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }
    
    // Dosya silme isteği (token ile birlikte)
    public class FileDeleteRequest
    {
        [Required]
        public int FileId { get; set; }
    }
} 