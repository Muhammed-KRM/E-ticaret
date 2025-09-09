using System.Diagnostics;
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

namespace KeremProject1backend.Operations
{
    public static class GeneralOperations
    {
        // Admin yetkisi kontrolü
        private static bool IsAdmin(string token, UsersContext usersContext)
        {
            var session = SessionService.TestToken(token, usersContext);
            return session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.AdminAdmin ||
                   session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.Admin;
        }

        public static BaseResponse GetAllConfigurationDataOperation(GetAllConfigurationDataRequest request, string token, UsersContext dbContext)
        {
            BaseResponse response = new();
            try
            {
                var currentSession = SessionService.TestToken(token, dbContext);
                if (currentSession == null)
                {
                    return response.GenerateError(1000, "Token geçersiz veya oturum süresi dolmuş.");
                }

                if (!SessionService.isAuthorized(currentSession, UserRoleinAuthorization.Admin))
                {
                    return response.GenerateError(1001, "Bu işlemi gerçekleştirme yetkiniz yok.");
                }

                // Config.UIConfigurationData artık enum'ları içeriyor
                response.Response = Config.UIConfigurationData;
                return response.GenerateSuccess();
            }
            catch (Exception ex)
            {
                return response.GenerateError(3005, ex.ToString());
            }
        }

        // Sistem Loglarını Getirme (Admin)
        public static async Task<BaseResponse> GetSystemLogsOperation(string token, GetSystemLogsRequest request, UsersContext dbContext, ILogger logger)
        {
            var response = new BaseResponse();
            try
            {
                if (!IsAdmin(token, dbContext)) // Pass dbContext
                {
                    response.GenerateError(1001, "Yetkisiz işlem. Admin girişi gereklidir.");
                    return response;
                }

                // LogServices içindeki statik metodu çağır
                var logs = LogServices.GetSystemLogs(request, dbContext);

                response.Response = logs; // Direkt List<DataLog> dönecek
                response.GenerateSuccess("Sistem logları başarıyla getirildi.");
            }
            catch (Exception ex)
            {
                 logger?.LogError(ex, "GetSystemLogsOperation sırasında hata oluştu.");
                 response.GenerateError(5701, $"Loglar getirilirken hata: {ex.Message}");
            }
            return response;
        }

        // İletişim Bilgilerini Getirme
        public static async Task<BaseResponse> GetContactInfoOperation(GeneralContext dbContext, ILogger logger)
        {
            var response = new BaseResponse();
            try
            {
                var contact = await dbContext.Contacts.OrderByDescending(c => c.Id).FirstOrDefaultAsync();

                if (contact == null)
                {
                    // Hiç iletişim bilgisi yoksa, varsayılan olarak bir kayıt oluştur
                    contact = new Contact
                    {
                        Address = "Örnek Adres, İstanbul, Türkiye",
                        Phone = "+90 (212) 123 45 67",
                        Email = "info@example.com",
                        WorkingHours = "Pazartesi - Cuma: 09:00 - 18:00",
                        Description = "Bize ulaşmak için yukarıdaki iletişim bilgilerini kullanabilirsiniz.",
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = 0 // Sistem tarafından
                    };

                    dbContext.Contacts.Add(contact);
                    await dbContext.SaveChangesAsync();
                    logger?.LogInformation("Varsayılan iletişim bilgileri oluşturuldu.");
                }

                var contactResponse = new ContactResponse
                {
                    Id = contact.Id,
                    Address = contact.Address,
                    Phone = contact.Phone,
                    Email = contact.Email,
                    WorkingHours = contact.WorkingHours,
                    Description = contact.Description,
                    UpdatedAt = contact.UpdatedAt
                };

                response.Response = contactResponse;
                response.GenerateSuccess("İletişim bilgileri başarıyla getirildi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "GetContactInfoOperation sırasında hata oluştu.");
                response.GenerateError(5800, $"İletişim bilgileri getirilirken hata: {ex.Message}");
            }
            return response;
        }

        // İletişim Bilgilerini Güncelleme (Admin)
        public static async Task<BaseResponse> UpdateContactInfoOperation(string token, UpdateContactRequest request, 
            GeneralContext dbContext, UsersContext usersContext, ILogger logger)
        {
            var response = new BaseResponse();
            try
            {
                // Oturum kontrolü
                var session = SessionService.TestToken(token, usersContext);
                if (session == null || session._user == null)
                {
                    response.GenerateError(1000, "Geçersiz oturum veya oturum süresi dolmuş.");
                    return response;
                }
                int userId = session._user.Id;

                if (!IsAdmin(token, usersContext))
                {
                    response.GenerateError(1001, "Yetkisiz işlem. Admin girişi gereklidir.");
                    return response;
                }

                // Mevcut iletişim bilgilerini bul
                var contact = await dbContext.Contacts.OrderByDescending(c => c.Id).FirstOrDefaultAsync();

                // Değişiklik öncesi durumu sakla (log için)
                var oldContact = contact != null ? new Contact
                {
                    Id = contact.Id,
                    Address = contact.Address,
                    Phone = contact.Phone,
                    Email = contact.Email,
                    WorkingHours = contact.WorkingHours,
                    Description = contact.Description,
                    UpdatedAt = contact.UpdatedAt,
                    UpdatedBy = contact.UpdatedBy
                } : null;

                // Eğer hiç kayıt yoksa yeni oluştur
                if (contact == null)
                {
                    contact = new Contact();
                    dbContext.Contacts.Add(contact);
                }

                // İletişim bilgilerini güncelle
                contact.Address = request.Address;
                contact.Phone = request.Phone;
                contact.Email = request.Email;
                contact.WorkingHours = request.WorkingHours;
                contact.Description = request.Description;
                contact.UpdatedAt = DateTime.UtcNow;
                contact.UpdatedBy = userId;

                await dbContext.SaveChangesAsync();
                logger?.LogInformation($"İletişim bilgileri güncellendi. Güncelleyen: UserId={userId}");

                // Güncelleme logunu kaydet
                await LogServices.AddLogAsync(
                    dbContext,
                    "Contacts",
                    oldContact == null ? 'C' : 'U', // Create veya Update
                    userId,
                    oldContact, // Önceki değer
                    contact // Yeni değer
                );

                var contactResponse = new ContactResponse
                {
                    Id = contact.Id,
                    Address = contact.Address,
                    Phone = contact.Phone,
                    Email = contact.Email,
                    WorkingHours = contact.WorkingHours,
                    Description = contact.Description,
                    UpdatedAt = contact.UpdatedAt
                };

                response.Response = contactResponse;
                response.GenerateSuccess("İletişim bilgileri başarıyla güncellendi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "UpdateContactInfoOperation sırasında hata oluştu.");
                response.GenerateError(5801, $"İletişim bilgileri güncellenirken hata: {ex.Message}");
            }
            return response;
        }

        // Kullanıcı Rapor Gönderme
        public static async Task<BaseResponse> SubmitReportOperation(string token, SubmitReportRequest request, GeneralContext dbContext, UsersContext usersContext, ILogger logger)
        {
            var response = new BaseResponse();
            try
            {
                var currentSession = SessionService.TestToken(token, usersContext);
                if (currentSession == null)
                {
                    response.GenerateError(1000, "Token geçersiz veya oturum süresi dolmuş.");
                    return response;
                }
                int userIdInt = currentSession._user.Id;

                var report = new Report
                {
                    UserId = userIdInt,
                    UserName = currentSession._user.UserName, // Kullanıcı adını ekle
                    Title = request.Title,
                    Content = request.Content,
                    CreatedAt = DateTime.UtcNow,
                    IsReviewed = false
                };

                dbContext.Reports.Add(report);
                await dbContext.SaveChangesAsync();
                logger?.LogInformation($"Yeni rapor gönderildi: UserId={userIdInt}, Title={request.Title}");

                response.GenerateSuccess("Raporunuz başarıyla gönderildi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "SubmitReportOperation sırasında hata oluştu.");
                response.GenerateError(5700, $"Rapor gönderilirken hata: {ex.Message}");
            }
            return response;
        }

        // Raporları Listeleme (Admin)
        public static async Task<BaseResponse> GetReportsOperation(string token, UsersContext usersContext, GeneralContext dbContext, ILogger logger)
        {
            var response = new BaseResponse();
            try
            {
                if (!IsAdmin(token, usersContext))
                {
                    response.GenerateError(1001, "Yetkisiz işlem. Admin girişi gereklidir.");
                    return response;
                }

                var reports = await dbContext.Reports
                                    .OrderByDescending(r => r.CreatedAt)
                                    .Select(r => new ReportResponse
                                    {
                                        Id = r.Id,
                                        UserId = r.UserId,
                                        UserName = r.UserName,
                                        Title = r.Title,
                                        Content = r.Content,
                                        CreatedAt = r.CreatedAt,
                                        IsReviewed = r.IsReviewed
                                    })
                                    .ToListAsync();

                response.Response = reports;
                response.GenerateSuccess("Raporlar başarıyla listelendi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "GetReportsOperation sırasında hata oluştu.");
                response.GenerateError(5702, $"Raporlar listelenirken hata: {ex.Message}");
            }
            return response;
        }

        // Raporu İncelendi Olarak İşaretleme (Admin)
        public static async Task<BaseResponse> MarkReportAsReviewedOperation(string token, int reportId, UsersContext usersContext, GeneralContext dbContext, ILogger logger)
        {
            var response = new BaseResponse();
            try
            {
                if (!IsAdmin(token, usersContext))
                {
                    response.GenerateError(1001, "Yetkisiz işlem. Admin girişi gereklidir.");
                    return response;
                }

                var report = await dbContext.Reports.FindAsync(reportId);
                if (report == null)
                {
                    response.GenerateError(4404, "Rapor bulunamadı.");
                    return response;
                }

                report.IsReviewed = true;
                await dbContext.SaveChangesAsync();
                logger?.LogInformation($"Rapor incelendi olarak işaretlendi: ReportId={reportId}");

                response.GenerateSuccess("Rapor başarıyla incelendi olarak işaretlendi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"MarkReportAsReviewedOperation sırasında hata oluştu: ReportId={reportId}");
                response.GenerateError(5703, $"Rapor işaretlenirken hata: {ex.Message}");
            }
            return response;
        }
    }
}
