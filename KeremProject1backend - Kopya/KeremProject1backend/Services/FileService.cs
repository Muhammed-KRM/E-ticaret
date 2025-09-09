using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace KeremProject1backend.Services
{
    public class FileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileService> _logger;
        private readonly string _uploadsFolder;
        
        // Yapılandırma ayarlarını da ekleyebilirsiniz
        // private readonly ConfigDef _config;

        public FileService(
            IWebHostEnvironment environment,
            ILogger<FileService> logger)
            // ConfigDef config)
        {
            _environment = environment;
            _logger = logger;
            // _config = config;
            
            // Uploads dizini oluşturma - eğer mevcutsa var olan kullanılır
            _uploadsFolder = Path.Combine(_environment.ContentRootPath, "Uploads");
            
            if (!Directory.Exists(_uploadsFolder))
            {
                Directory.CreateDirectory(_uploadsFolder);
            }
        }

        // Dosya yükleme işlemi
        public async Task<FileUploadResponse> UploadFileAsync(IFormFile file, string? fileType = null, int? relatedEntityId = null, string? description = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("Geçerli bir dosya yüklenmedi.");
                }

                // Güvenli dosya adı oluştur
                string originalFileName = Path.GetFileName(file.FileName);
                string fileExtension = Path.GetExtension(originalFileName);
                string uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                
                // Dosya tipine göre alt klasör oluştur
                string subFolder = !string.IsNullOrEmpty(fileType) ? fileType : "genel";
                
                // ProductImage ve product-images tutarlılığı için
                if (subFolder == "product-images")
                {
                    subFolder = "ProductImage";
                }
                
                string folderPath = Path.Combine(_uploadsFolder, subFolder);
                
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                
                string filePath = Path.Combine(folderPath, uniqueFileName);
                
                // Dosyayı fiziksel olarak kaydet
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                
                // Dosyanın veritabanında kaydedilecek göreceli yolu
                string relativePath = Path.Combine("Uploads", subFolder, uniqueFileName);
                
                // URL oluştur
                string fileUrl = uniqueFileName; // Artık sadece dosya adını saklıyoruz
                
                var response = new FileUploadResponse
                {
                    FileName = uniqueFileName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    FileType = fileType ?? string.Empty,
                    FilePath = relativePath,
                    UploadDate = DateTime.Now,
                    Description = description ?? string.Empty,
                    FileUrl = fileUrl
                };
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya yükleme sırasında hata oluştu.");
                throw;
            }
        }
        
        // Dosyayı fiziksel olarak okuma
        public async Task<byte[]> ReadFileAsync(string relativePath)
        {
            string fullPath = Path.Combine(_environment.ContentRootPath, relativePath);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Dosya bulunamadı", relativePath);
            }
            
            return await File.ReadAllBytesAsync(fullPath);
        }
        
        // Dosyayı fiziksel olarak silme
        public async Task DeleteFileAsync(string relativePath)
        {
            string fullPath = Path.Combine(_environment.ContentRootPath, relativePath);
            
            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
            }
        }
        
        // Dosya URL'i oluşturma
        public string GetFileUrl(int fileId, string baseUrl)
        {
            return $"{baseUrl}/api/File/Download/{fileId}";
        }
        
        // İçerik kök dizinini al
        public string GetRootPath()
        {
            return _environment.ContentRootPath;
        }

        // Byte dizisinden dosya yükleme işlemi (yeni eklendi)
        public async Task<FileUploadResponse> UploadFileFromBytesAsync(byte[] fileBytes, string fileName, string? fileType = null, int? relatedEntityId = null, string? description = null)
        {
            try
            {
                if (fileBytes == null || fileBytes.Length == 0)
                {
                    throw new ArgumentException("Geçerli bir dosya verisi (byte dizisi) sağlanmadı.");
                }
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    throw new ArgumentException("Dosya adı belirtilmelidir.");
                }

                // Güvenli dosya adı oluştur
                string originalFileName = Path.GetFileName(fileName); // Kullanıcıdan gelen dosya adını al
                string fileExtension = Path.GetExtension(originalFileName);
                string uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                
                // Dosya tipine göre alt klasör oluştur
                string subFolder = !string.IsNullOrEmpty(fileType) ? fileType : "genel";
                
                // ProductImage ve product-images tutarlılığı için
                if (subFolder == "product-images")
                {
                    subFolder = "ProductImage";
                }
                
                string folderPath = Path.Combine(_uploadsFolder, subFolder);
                
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                
                string filePath = Path.Combine(folderPath, uniqueFileName);
                
                // Dosyayı fiziksel olarak kaydet
                await File.WriteAllBytesAsync(filePath, fileBytes);
                
                // Dosyanın veritabanında kaydedilecek göreceli yolu
                string relativePath = Path.Combine("Uploads", subFolder, uniqueFileName);
                
                // URL oluştur (sadece dosya adı)
                string fileUrl = uniqueFileName;

                // ContentType belirleme (MIME type)
                // Basit bir çıkarım yapabiliriz veya daha kapsamlı bir kütüphane kullanılabilir.
                // Şimdilik dosya uzantısına göre temel bir contentType atayalım.
                string contentType = fileExtension.ToLowerInvariant() switch
                {
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".pdf" => "application/pdf",
                    _ => "application/octet-stream" // Bilinmeyenler için genel tip
                };
                
                var response = new FileUploadResponse
                {
                    FileName = uniqueFileName,
                    ContentType = contentType, // Belirlenen ContentType
                    FileSize = fileBytes.Length,
                    FileType = fileType ?? string.Empty,
                    FilePath = relativePath,
                    UploadDate = DateTime.Now,
                    Description = description ?? string.Empty,
                    FileUrl = fileUrl
                };
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Byte dizisinden dosya yükleme sırasında hata oluştu.");
                throw;
            }
        }
    }
} 