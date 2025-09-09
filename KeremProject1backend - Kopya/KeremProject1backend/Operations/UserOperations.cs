using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using KeremProject1backend.Services;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KeremProject1backend.Operations
{
    public class UserOperations
    {
        // Admin yetkisi kontrolü için yardımcı metot
        private static bool IsAdmin(string token, UsersContext usersContext) // Add UsersContext
        {
            var session = SessionService.TestToken(token, usersContext); // Pass usersContext
            return session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.AdminAdmin ||
                   session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.Admin;
        }

        public static BaseResponse GeAllUser(GetAllUserRequests request, string token, UsersContext dbctx)
        {
            BaseResponse response = new();
            try
            {
                // Yetki kontrolü
                if (!IsAdmin(token, dbctx)) // Use IsAdmin helper
                {
                    return response.GenerateError(1001, "Bu işlemi gerçekleştirme yetkiniz yok.");
                }

                var users = dbctx.AllUsersModel.ToList();

                // Response oluşturma
                response.Response = users;


                return response.GenerateSuccess();
            }
            catch (Exception ex)
            {
                return response.GenerateError(2003, $"Beklenmeyen hata: {ex.Message}");
            }
        }

        public static BaseResponse DeleteUser(UserDeleteRequests request, string token, UsersContext dbctx)
        {
            BaseResponse response = new();
            try
            {
                // Yetki kontrolü
                if (!IsAdmin(token, dbctx)) // Use IsAdmin helper
                {
                    return response.GenerateError(1001, "Bu işlemi gerçekleştirme yetkiniz yok.");
                }

                var deleteUser = dbctx.AllUsersModel.Where(e=> e.Id == request.Id).FirstOrDefault();
                if (deleteUser == null)
                    return response.GenerateError(1081, "Kullanıcı bulnamadı.");


                dbctx.AllUsersModel.RemoveRange(deleteUser);
                dbctx.SaveChanges();

                return response.GenerateSuccess("Kullanıcı başarıyla silindi");
            }
            catch (Exception ex)
            {
                return response.GenerateError(2003, $"Beklenmeyen hata: {ex.Message}");
            }
        }

        public static BaseResponse UpdateUser(UpdateUserRequests request, string token, UsersContext dbctx)
        {
            BaseResponse response = new();
            try
            {
                // Yetki kontrolü
                if (!IsAdmin(token, dbctx)) // Use IsAdmin helper
                {
                    return response.GenerateError(1001, "Bu işlemi gerçekleştirme yetkiniz yok.");
                }

                // Token kontrolü ve session alma (ModUser için gerekli)
                var currentSession = SessionService.TestToken(token, dbctx); 
                if (currentSession == null)
                {
                    return response.GenerateError(1000, "Token geçersiz veya oturum süresi dolmuş.");
                }

                var updateUserEntity = dbctx.AllUsersModel.FirstOrDefault(e => e.Id == request.Id);
                if (updateUserEntity == null)
                    return response.GenerateError(1023,"Kullanıcı bulunamadı");

                // Update user fields
                updateUserEntity.UserName = request.UserName;
                updateUserEntity.PasswordHash = request.PasswordHash; 
                updateUserEntity.UserImageLink = request.UserImageLink;
                updateUserEntity.Email = request.Email;
                updateUserEntity.PhoneNumber = request.PhoneNumber;
                updateUserEntity.Address = request.Address;
                updateUserEntity.City = request.City;
                updateUserEntity.District = request.District;
                updateUserEntity.PostalCode = request.PostalCode;
                updateUserEntity.Country = request.Country;
                updateUserEntity.UserRoleinAuthorization = request.UserRoleinAuthorization; 
                updateUserEntity.State = request.State;
                updateUserEntity.ModTime = DateTime.Now;
                updateUserEntity.ModUser = currentSession._user.Id; 

                dbctx.AllUsersModel.Update(updateUserEntity);
                dbctx.SaveChanges();
                
                // Response oluşturma (Güncellenmiş veriyi içeren)
                response.Response = new UpdateUserResponses
                {
                    Id = updateUserEntity.Id,
                    UserName = updateUserEntity.UserName,
                    PasswordHash= updateUserEntity.PasswordHash,
                    UserImageLink = updateUserEntity.UserImageLink,
                    Email = updateUserEntity.Email,
                    PhoneNumber = updateUserEntity.PhoneNumber,
                    Address = updateUserEntity.Address,
                    City = updateUserEntity.City,
                    District = updateUserEntity.District,
                    PostalCode = updateUserEntity.PostalCode,
                    Country = updateUserEntity.Country,
                    UserRoleinAuthorization = updateUserEntity.UserRoleinAuthorization,
                    State = (bool)updateUserEntity.State,
                    ModTime = updateUserEntity.ModTime.Value,
                    ModUser = updateUserEntity.ModUser.Value
                };
                
                //log (Loglama gerekliyse devam edebilir)
                // List<DataLog> dataLogList = new List<DataLog>();
                // ... log creation ...
                // dataLogList.Add(logEntry);
                response.SetUserID(updateUserEntity.Id);
                        
                return response.GenerateSuccess("Kullanıcı güncellendi.");
            }
            catch (Exception ex)
            {
                return response.GenerateError(2003, $"Beklenmeyen hata: {ex.Message}");
            }
        }

        public static async Task<BaseResponse> UpdateMyProfileOperation(UpdateMyProfileRequest request, string token, UsersContext dbctx, ILogger? logger = null)
        {
            BaseResponse response = new();
            try
            {
                // Token kontrolü ve session alma
                var currentSession = SessionService.TestToken(token, dbctx);
                if (currentSession == null)
                {
                    return response.GenerateError(1000, "Token geçersiz veya oturum süresi dolmuş.");
                }

                // Kullanıcının kendi bilgilerini güncelleyebilmesi için sadece kendi ID'sini kullanıyoruz
                var user = await dbctx.AllUsersModel.FindAsync(currentSession._user.Id);
                if (user == null)
                {
                    return response.GenerateError(4004, "Kullanıcı bulunamadı.");
                }

                // Kullanıcı adı değiştirilmek isteniyorsa, aynı kullanıcı adının başka biri tarafından kullanılıp kullanılmadığını kontrol et
                if (!string.IsNullOrEmpty(request.UserName) && request.UserName != user.UserName)
                {
                    bool usernameExists = await dbctx.AllUsersModel.AnyAsync(u => u.UserName == request.UserName && u.Id != user.Id);
                    if (usernameExists)
                    {
                        return response.GenerateError(1101, "Bu kullanıcı adı zaten kullanılıyor.");
                    }
                    user.UserName = request.UserName;
                }

                // Diğer bilgileri güncelle (sadece null olmayan değerler)
                if (!string.IsNullOrEmpty(request.UserImageLink))
                {
                    user.UserImageLink = request.UserImageLink;
                }

                if (!string.IsNullOrEmpty(request.Email))
                {
                    user.Email = request.Email;
                }

                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    user.PhoneNumber = request.PhoneNumber;
                }

                if (!string.IsNullOrEmpty(request.Address))
                {
                    user.Address = request.Address;
                }

                if (!string.IsNullOrEmpty(request.City))
                {
                    user.City = request.City;
                }

                if (!string.IsNullOrEmpty(request.District))
                {
                    user.District = request.District;
                }

                if (!string.IsNullOrEmpty(request.PostalCode))
                {
                    user.PostalCode = request.PostalCode;
                }

                if (!string.IsNullOrEmpty(request.Country))
                {
                    user.Country = request.Country;
                }

                // Güncelleme bilgilerini ayarla
                user.ModTime = DateTime.UtcNow;
                user.ModUser = user.Id; // Kendi kendini güncelliyor

                await dbctx.SaveChangesAsync();
                logger?.LogInformation($"Kullanıcı profili güncellendi: UserId={user.Id}, UserName={user.UserName}");

                // Log kaydı oluştur
                LogServices.AddLog(
                    dbctx,
                    "AllUsersModel",
                    'U', // Update
                    user.Id,
                    null, // Önceki değer (şimdilik null)
                    new { // Güncellenmiş değerler (şifre hash'i olmadan)
                        Id = user.Id,
                        UserName = user.UserName,
                        UserImageLink = user.UserImageLink,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Address = user.Address,
                        City = user.City,
                        District = user.District,
                        PostalCode = user.PostalCode,
                        Country = user.Country,
                        ModTime = user.ModTime
                    }
                );

                // Response oluştur
                var profileResponse = new UpdateMyProfileResponse
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    UserImageLink = user.UserImageLink,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    City = user.City,
                    District = user.District,
                    PostalCode = user.PostalCode,
                    Country = user.Country,
                    UserRoleinAuthorization = user.UserRoleinAuthorization,
                    ModTime = user.ModTime
                };

                response.Response = profileResponse;
                response.SetUserID(user.Id);
                return response.GenerateSuccess("Profil bilgileriniz başarıyla güncellendi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "UpdateMyProfileOperation sırasında hata oluştu.");
                return response.GenerateError(5102, $"Profil güncellenirken hata: {ex.Message}");
            }
        }
    }
}
