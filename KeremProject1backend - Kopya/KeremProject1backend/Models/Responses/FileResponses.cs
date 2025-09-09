using System;

namespace KeremProject1backend.Models.Responses
{
    // Dosya yükleme yanıtı
    public class FileUploadResponse
    {
        public int FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileType { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; } = DateTime.Now;
        public string Description { get; set; } = string.Empty;
        
        // Dosya için URL oluşturmak amacıyla
        public string FileUrl { get; set; } = string.Empty;
    }
    
    // Çoklu dosya yükleme yanıtı
    public class MultipleFileUploadResponse
    {
        public FileUploadResponse[] UploadedFiles { get; set; } = Array.Empty<FileUploadResponse>();
    }
    
    // Dosya indirme yanıtı
    public class FileDownloadResponse
    {
        public int FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public byte[] FileData { get; set; } = Array.Empty<byte>();
    }
    
    // Dosya bilgisi yanıtı (indirme olmadan)
    public class FileInfoResponse
    {
        public int FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileType { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; } = DateTime.Now;
        public string Description { get; set; } = string.Empty;
        
        // Dosya için URL oluşturmak amacıyla
        public string FileUrl { get; set; } = string.Empty;
    }
} 