using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using KeremProject1backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace KeremProject1backend.Operations
{
    public static class FileOperations
    {
        // Admin yetkisi kontrolü için yardımcı metot
        private static bool IsAdmin(string token, UsersContext usersContext)
        {
            var session = SessionService.TestToken(token, usersContext);
            return session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.AdminAdmin || 
                   session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.Admin;
        }
        
        // Kullanıcı yetkisi kontrolü (dosya işlemleri için)
        private static bool IsAuthorized(string token, UsersContext usersContext, FileModel file)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            var session = SessionService.TestToken(token, usersContext);
            
            // Admin her zaman yetkilidir
            if (session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.AdminAdmin || 
                session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.Admin)
            {
                return true;
            }
            
            // Kullanıcı kendi yüklediği dosyalara erişebilir
            return session?._user?.Id == file.UploadedByUserId;
        }

        // Dosya yükleme
        public static async Task<BaseResponse> UploadFileOperation(
            FileUploadRequest request, 
            string token, 
            FileService fileService, 
            UsersContext usersContext, 
            ILogger logger)
        {
            var response = new BaseResponse();
            
            try
            {
                // Yetki kontrolü (opsiyonel, ihtiyaca göre değiştirilebilir)
                int userId = 0;
                if (!string.IsNullOrEmpty(token))
                {
                    var session = SessionService.TestToken(token, usersContext);
                    if (session == null)
                    {
                        // Token geçersiz ama isteğe bağlı olabilir
                        logger?.LogWarning("Geçersiz token ile dosya yükleme denemesi.");
                    }
                    else
                    {
                        userId = session._user?.Id ?? 0;
                    }
                }
                
                if (request.File == null || request.File.Length == 0)
                {
                    response.GenerateError(4000, "Geçerli bir dosya yüklenmedi.");
                    return response;
                }
                
                // Dosya yükleme işlemini servis üzerinden gerçekleştir
                var fileUploadResult = await fileService.UploadFileAsync(
                    request.File, 
                    request.FileType, 
                    request.RelatedEntityId, 
                    request.Description);
                
                // Burada veritabanına kaydedilecek işlemler yapılacak, şimdilik sadece yanıt döndürelim
                fileUploadResult.FileId = 1; // Veritabanına kaydedilince gerçek ID alacak
                
                // Log kayıt işlemi 
                if (userId > 0)
                {
                    await LogServices.AddLogAsync(
                        usersContext, 
                        "Files", 
                        'C', // Create
                        userId,
                        null, // Önceki değer yok
                        fileUploadResult // Yeni eklenen dosya bilgileri
                    );
                }
                
                response.Response = fileUploadResult;
                response.GenerateSuccess("Dosya başarıyla yüklendi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "UploadFileOperation sırasında hata oluştu.");
                response.GenerateError(5000, $"Dosya yüklenirken hata oluştu: {ex.Message}");
            }
            
            return response;
        }
        
        //// Çoklu dosya yükleme
        public static async Task<BaseResponse> UploadMultipleFilesOperation(
            MultipleFileUploadRequest request, 
            string token, 
            FileService fileService, 
            UsersContext usersContext, 
            ILogger logger)
        {
            var response = new BaseResponse();
            
            try
            {
                // Yetki kontrolü (opsiyonel, ihtiyaca göre değiştirilebilir)
                int? userId = null;
                if (!string.IsNullOrEmpty(token))
                {
                    var session = SessionService.TestToken(token, usersContext);
                    if (session == null)
                    {
                        // Token geçersiz ama isteğe bağlı olabilir
                        logger?.LogWarning("Geçersiz token ile çoklu dosya yükleme denemesi.");
                    }
                    else
                    {
                        userId = session._user?.Id;
                    }
                }
                
                if (request.Files == null || !request.Files.Any())
                {
                    response.GenerateError(4000, "Geçerli dosyalar yüklenmedi.");
                    return response;
                }
                
                var uploadedFiles = new List<FileUploadResponse>();
                
                // Her dosya için yükleme işlemi yap
                foreach (var file in request.Files)
                {
                    if (file.Length == 0)
                    {
                        continue;
                    }
                    
                    var fileUploadResult = await fileService.UploadFileAsync(
                        file, 
                        request.FileType, 
                        request.RelatedEntityId, 
                        request.Description);
                    
                    // Veritabanına kaydedilince gerçek ID alacak, şimdilik örnek
                    fileUploadResult.FileId = uploadedFiles.Count + 1;
                    uploadedFiles.Add(fileUploadResult);
                }
                
                var multipleFileResponse = new MultipleFileUploadResponse
                {
                    UploadedFiles = uploadedFiles.ToArray()
                };
                
                response.Response = multipleFileResponse;
                response.GenerateSuccess($"{uploadedFiles.Count} dosya başarıyla yüklendi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "UploadMultipleFilesOperation sırasında hata oluştu.");
                response.GenerateError(5001, $"Dosyalar yüklenirken hata oluştu: {ex.Message}");
            }
            
            return response;
        }
        
        // Dosya indirme
        public static async Task<BaseResponse> DownloadFileOperation(
            string fileName,
            string token, 
            FileService fileService, 
            UsersContext usersContext, 
            GeneralContext dbContext, 
            ILogger logger)
        {
            var response = new BaseResponse();
            
            try
            {
                logger?.LogInformation($"DownloadFileOperation: Dosya adı: '{fileName}'");
                
                // Dosya bilgilerini veritabanından fileName'e göre al
                var file = await dbContext.Files.FirstOrDefaultAsync(f => f.FileName == fileName);
                
                if (file == null)
                {
                    // Veritabanında dosya bulunamadı, dosya sistemi kontrolü yap
                    logger?.LogWarning($"Veritabanında '{fileName}' dosyası bulunamadı. Dosya sistemi kontrolü yapılıyor.");
                    
                    // Dosyanın muhtemel yerlerini kontrol et
                    string[] possibleFolders = new[] { "ProductImage", "product-images", "png", "genel" };
                    string? foundFilePath = null;
                    
                    foreach (var folder in possibleFolders)
                    {
                        string relativePath = Path.Combine("Uploads", folder, fileName);
                        string fullPath = Path.Combine(fileService.GetRootPath(), relativePath);
                        
                        if (File.Exists(fullPath))
                        {
                            foundFilePath = relativePath;
                            logger?.LogInformation($"Dosya sisteminde bulundu: {fullPath}");
                            break;
                        }
                    }
                    
                    if (foundFilePath == null)
                    {
                        response.GenerateError(4404, "Dosya bulunamadı.");
                        return response;
                    }
                    
                    // Dosya bulundu ama veritabanında yok, geçici bir FileModel oluştur
                    string contentType = GetContentTypeFromFileName(fileName);
                    file = new FileModel 
                    {
                        FileName = fileName,
                        ContentType = contentType,
                        FilePath = foundFilePath,
                        FileSize = new FileInfo(Path.Combine(fileService.GetRootPath(), foundFilePath)).Length
                    };
                }
                
                // Yetki kontrolü (bazı dosyalar herkese açık olabilir, bu senaryoya göre ayarlanabilir)
                bool requiresAuth = false; // Örnek, tüm dosyaların herkese açık olduğunu varsayalım
                
                if (requiresAuth && !IsAuthorized(token, usersContext, file))
                {
                    response.GenerateError(4403, "Bu dosyaya erişim yetkiniz yok.");
                    return response;
                }
                
                try
                {
                    // Dosyayı diskten oku
                    byte[] fileData = await fileService.ReadFileAsync(file.FilePath);
                    
                    var fileResponse = new FileDownloadResponse
                    {
                        FileId = file.Id,
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        FileSize = file.FileSize,
                        FileData = fileData
                    };
                    
                    response.Response = fileResponse;
                    response.GenerateSuccess("Dosya başarıyla indirildi.");
                }
                catch (FileNotFoundException ex)
                {
                    logger?.LogWarning(ex, $"Fiziksel dosya bulunamadı (Yol: {file.FilePath}).");
                    response.GenerateError(4404, "Dosya fiziksel olarak bulunamadı.");
                }
            }
            catch (FileNotFoundException ex)
            {
                logger?.LogWarning(ex, $"Fiziksel dosya bulunamadı (Ad: {fileName}).");
                response.GenerateError(4404, "Dosya bulunamadı.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"DownloadFileOperation sırasında hata oluştu (Ad: {fileName}).");
                response.GenerateError(5002, $"Dosya indirilirken hata oluştu: {ex.Message}");
            }
            
            return response;
        }
        
        // Dosya adından MIME tipini tahmin et
        private static string GetContentTypeFromFileName(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".txt" => "text/plain",
                ".zip" => "application/zip",
                _ => "application/octet-stream" // Bilinmeyen
            };
        }
        
        // Dosya silme
        public static async Task<BaseResponse> DeleteFileOperation(
            int fileId, 
            string token, 
            FileService fileService, 
            UsersContext usersContext, 
            GeneralContext dbContext, 
            ILogger logger)
        {
            var response = new BaseResponse();
            
            try
            {
                // Yetki kontrolü
                if (string.IsNullOrEmpty(token))
                {
                    response.GenerateError(4401, "Bu işlem için yetkilendirme gerekli.");
                    return response;
                }
                
                var session = SessionService.TestToken(token, usersContext);
                if (session == null || session._user == null)
                {
                    response.GenerateError(4401, "Geçersiz token veya oturum süresi dolmuş.");
                    return response;
                }
                int userId = session._user.Id;
                
                // Dosya bilgilerini veritabanından al
                var file = await dbContext.Files.FindAsync(fileId);
                
                if (file == null)
                {
                    response.GenerateError(4404, "Silinecek dosya bulunamadı.");
                    return response;
                }
                
                // Yetki kontrolü
                if (!IsAuthorized(token, usersContext, file))
                {
                    response.GenerateError(4403, "Bu dosyayı silme yetkiniz yok.");
                    return response;
                }
                
                // Silmeden önce mevcut dosya bilgilerini sakla (log için)
                var oldFile = new FileModel
                {
                    Id = file.Id,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.FileSize,
                    FilePath = file.FilePath,
                    FileType = file.FileType,
                    RelatedEntityId = file.RelatedEntityId,
                    Description = file.Description,
                    UploadDate = file.UploadDate,
                    IsActive = file.IsActive
                };
                
                // Dosyayı fiziksel olarak sil
                await fileService.DeleteFileAsync(file.FilePath);
                
                // Veritabanından da sil
                dbContext.Files.Remove(file);
                await dbContext.SaveChangesAsync();
                
                // Silme işlemi logunu kaydet
                await LogServices.AddLogAsync(
                    dbContext,
                    "Files",
                    'D', // Delete
                    userId,
                    oldFile, // Silinen dosya bilgileri
                    null // Yeni değer yok
                );
                
                response.GenerateSuccess("Dosya başarıyla silindi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"DeleteFileOperation sırasında hata oluştu (ID: {fileId}).");
                response.GenerateError(5003, $"Dosya silinirken hata oluştu: {ex.Message}");
            }
            
            return response;
        }
        
        // Dosya bilgilerini getirme
        public static async Task<BaseResponse> GetFileInfoOperation(
            int fileId, 
            GeneralContext dbContext, 
            ILogger logger)
        {
            var response = new BaseResponse();
            
            try
            {
                // Dosya bilgilerini veritabanından al
                var file = await dbContext.Files.FindAsync(fileId);
                
                if (file == null)
                {
                    response.GenerateError(4404, "Dosya bulunamadı.");
                    return response;
                }
                
                var fileInfo = new FileInfoResponse
                {
                    FileId = file.Id,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.FileSize,
                    FileType = file.FileType,
                    UploadDate = file.UploadDate,
                    Description = file.Description,
                    FileUrl = $"/api/File/Download/{file.FileName}"
                };
                
                response.Response = fileInfo;
                response.GenerateSuccess("Dosya bilgileri başarıyla getirildi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"GetFileInfoOperation sırasında hata oluştu (ID: {fileId}).");
                response.GenerateError(5004, $"Dosya bilgileri alınırken hata oluştu: {ex.Message}");
            }
            
            return response;
        }
    }
} 