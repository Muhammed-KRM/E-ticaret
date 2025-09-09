using System.Text.Json;
using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using KeremProject1backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace KeremProject1backend.Operations
{
    public static class AuthOperations
    {
        private static readonly PasswordHasher<AllUsersModels> _passwordHasher = new PasswordHasher<AllUsersModels>();

        public static async Task<BaseResponse> LoginOperation(LoginRequest request, UsersContext dbContext, ILogger logger)
        {
            var response = new BaseResponse();
            try
            {
                logger?.LogInformation($"Login denemesi: Kullanıcı Adı='{request.UserName}'");
                // Kullanıcıyı bulmaya çalış
                var user = await dbContext.AllUsersModel.FirstOrDefaultAsync(u => u.UserName == request.UserName);

                if (user == null)
                {
                    logger?.LogWarning($"Login Başarısız: Kullanıcı bulunamadı -> '{request.UserName}'");
                    response.GenerateError(1001, "Kullanıcı adı veya şifre hatalı.");
                    return response;
                }

                logger?.LogInformation($"Login: Kullanıcı bulundu -> ID={user.Id}, KullanıcıAdı='{user.UserName}'");

                // Şifreyi doğrula
                var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

                if (passwordVerificationResult == PasswordVerificationResult.Failed)
                {
                    logger?.LogWarning($"Login Başarısız: Şifre doğrulaması başarısız -> Kullanıcı ID={user.Id}, KullanıcıAdı='{user.UserName}'");
                    response.GenerateError(1001, "Kullanıcı adı veya şifre hatalı.");
                    return response;
                }

                logger?.LogInformation($"Login Başarılı: Şifre doğrulandı -> Kullanıcı ID={user.Id}, KullanıcıAdı='{user.UserName}'");

                // Token oluştur ve yanıtı hazırla
                var token = TokenServices.GenerateToken(user.Id, user.UserRoleinAuthorization.ToString());
                var loginResponse = new LoginResponse
                {
                    Token = token,
                    UserId = user.Id,
                    UserName = user.UserName,
                    UserRoleinAuthorization = user.UserRoleinAuthorization,
                    UserImageLink = user.UserImageLink ?? string.Empty,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    City = user.City,
                    District = user.District,
                    PostalCode = user.PostalCode,
                    Country = user.Country
                };

                response.Response = loginResponse;
                response.GenerateSuccess("Giriş başarılı.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"LoginOperation sırasında hata oluştu. Kullanıcı Adı: '{request.UserName}'");
                response.GenerateError(5000, $"Giriş yapılırken hata: {ex.Message}");
            }
            return response;
        }

        public static BaseResponse GetMyInfoOperation(string token, UsersContext dbContext)
        {
            var response = new BaseResponse();
            try
            {
                var currentSession = SessionService.TestToken(token, dbContext);
                if (currentSession == null)
                {
                    response.GenerateError(1000, "Token geçersiz veya oturum süresi dolmuş.");
                    return response;
                }
                response.Response = currentSession._user;
                response.GenerateSuccess("Kullanıcı bilgileri başarıyla getirildi.");
            }
            catch (Exception ex)
            {
                response.GenerateError(5001, $"Kullanıcı bilgileri getirilirken hata: {ex.Message}");
            }
             return response;
        }

        public static async Task<BaseResponse> RegisterAdminOperation(string token, RegisterAdminRequest request, UsersContext dbContext, ILogger logger)
        {
            var response = new BaseResponse();
            try
            {
                var currentSession = SessionService.TestToken(token, dbContext);
                if (currentSession == null)
                {
                    response.GenerateError(1000, "Token geçersiz veya oturum süresi dolmuş.");
                    return response;
                }

                if (currentSession._user.UserRoleinAuthorization != UserRoleinAuthorization.AdminAdmin)
                {
                    response.GenerateError(1002, "Bu işlemi yapma yetkiniz yok.");
                    return response;
                }

                bool usernameExists = await dbContext.AllUsersModel.AnyAsync(u => u.UserName == request.Username);
                if (usernameExists)
                {
                    response.GenerateError(1101, "Bu kullanıcı adı zaten kullanılıyor.");
                    return response;
                }

                var userToHash = new AllUsersModels { UserName = request.Username };
                var hashedPassword = _passwordHasher.HashPassword(userToHash, request.Password);

                var newAdmin = new AllUsersModels
                {
                    UserName = request.Username,
                    PasswordHash = hashedPassword,
                    UserImageLink = request.UserImageLink,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    UserRoleinAuthorization = UserRoleinAuthorization.Admin,
                    State = true,
                    ModTime = DateTime.UtcNow,
                    ModUser = currentSession._user.Id
                };

                dbContext.AllUsersModel.Add(newAdmin);
                await dbContext.SaveChangesAsync();

                logger?.LogInformation($"Yeni admin kullanıcı kaydedildi: {newAdmin.UserName}, Kaydeden: {currentSession._user.UserName}");

                response.Response = new { UserId = newAdmin.Id, UserName = newAdmin.UserName };
                response.GenerateSuccess("Admin başarıyla kaydedildi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "RegisterAdminOperation sırasında hata oluştu.");
                response.GenerateError(5100, $"Admin kaydedilirken hata: {ex.Message}");
            }
            return response;
        }

        public static async Task<BaseResponse> UpdatePasswordOperation(string token, UpdatePasswordRequest request, UsersContext dbContext, ILogger logger)
        {
            var response = new BaseResponse();
            try
            {
                var currentSession = SessionService.TestToken(token, dbContext);
                if (currentSession == null)
                {
                    response.GenerateError(1000, "Token geçersiz veya oturum süresi dolmuş.");
                    return response;
                }

                var user = await dbContext.AllUsersModel.FindAsync(currentSession._user.Id);
                if (user == null)
                {
                    response.GenerateError(4004, "Kullanıcı bulunamadı.");
                    return response;
                }

                var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.OldPassword);
                if (passwordVerificationResult == PasswordVerificationResult.Failed)
                {
                    response.GenerateError(1102, "Mevcut şifreniz hatalı.");
                    return response;
                }

                var hashedNewPassword = _passwordHasher.HashPassword(user, request.NewPassword);

                user.PasswordHash = hashedNewPassword;
                user.ModTime = DateTime.UtcNow;
                user.ModUser = user.Id;

                await dbContext.SaveChangesAsync();
                logger?.LogInformation($"Kullanıcı şifresi güncellendi: UserId={user.Id}");

                response.GenerateSuccess("Şifreniz başarıyla güncellendi.");

            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "UpdatePasswordOperation sırasında hata oluştu.");
                response.GenerateError(5101, $"Şifre güncellenirken hata: {ex.Message}");
            }
            return response;
        }

        public static BaseResponse LogoutOperation(string token, UsersContext dbctx)
        {
            BaseResponse response = new();

            try
            {
                var currentSession = SessionService.TestToken(token, dbctx);

                if (currentSession == null)
                {
                    return response.GenerateError(1000, "Geçersiz veya süresi dolmuş token.");
                }
                var user = dbctx.AllUsersModel.FirstOrDefault(u => u.Id == currentSession._user.Id);
                if (user != null)
                {
                    // Oturum kapatıldığında kullanıcı state'ini değiştirmek yerine
                    // belki sadece session kaydını silmek daha uygun olabilir.
                    // Şimdilik mevcut haliyle bırakıyorum.
                    // user.State = false; 
                    // dbctx.SaveChanges();
                }

                // SessionService tarafında token geçersiz kılma/silme işlemi yapılabilir.
                // SessionService.InvalidateToken(token);

                response.SetUserID(currentSession._user.Id);
                return response.GenerateSuccess("Logout başarılı.");
            }
            catch (Exception ex)
            {
                 // Loglama eklenebilir: logger?.LogError(ex, "LogoutOperation sırasında hata.");
                return response.GenerateError(2000, $"Beklenmeyen hata: {ex.Message}");
            }
        }

        public static BaseResponse SignUpOperation(SignUpRequest request, UsersContext dbctx)
        {
            BaseResponse response = new();

            try
            {
                if (dbctx.AllUsersModel.Any(u => u.UserName == request.UserName))
                {
                    return response.GenerateError(1006, "Bu kullanıcı adı zaten alınmış.");
                }
                var userToHash = new AllUsersModels { UserName = request.UserName };
                var hashedPassword = _passwordHasher.HashPassword(userToHash, request.Password);

                var newUser = new AllUsersModels
                {
                    UserName = request.UserName,
                    PasswordHash = hashedPassword,
                    UserImageLink = request.UserImageLink,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Address = request.Address,
                    City = request.City,
                    District = request.District,
                    PostalCode = request.PostalCode,
                    Country = request.Country,
                    UserRoleinAuthorization = UserRoleinAuthorization.User,
                    State = true,
                    ModTime = DateTime.UtcNow,
                    ModUser = 0
                };

                dbctx.AllUsersModel.Add(newUser);
                dbctx.SaveChanges();

                // Sisteme yeni kullanıcı kaydı log'u
                LogServices.AddLog(
                    dbctx,
                    "AllUsersModel",
                    'C', // Create
                    newUser.Id, // Kendi kaydının log'unu kendisi oluşturuyor
                    null, // Önceki değer yok
                    new { // Şifre hash'i olmadan kullanıcı bilgilerini logla
                        Id = newUser.Id,
                        UserName = newUser.UserName,
                        UserRole = newUser.UserRoleinAuthorization,
                        UserImageLink = newUser.UserImageLink,
                        Email = newUser.Email,
                        PhoneNumber = newUser.PhoneNumber,
                        Address = newUser.Address,
                        City = newUser.City,
                        District = newUser.District,
                        PostalCode = newUser.PostalCode,
                        Country = newUser.Country,
                        State = newUser.State,
                        ModTime = newUser.ModTime
                    }
                );

                var signUpResponse = new SignUpResponse
                {
                    UserId = newUser.Id,
                    UserName = newUser.UserName,
                    Email = newUser.Email,
                    UserRole = newUser.UserRoleinAuthorization.ToString()
                };

                response.Response = signUpResponse;
                response.SetUserID(newUser.Id);
                return response.GenerateSuccess("Kayıt işlemi başarılı.");
            }
            catch (Exception ex)
            {
                // Loglama eklenebilir: logger?.LogError(ex, "SignUpOperation sırasında hata.");
                return response.GenerateError(2001, $"Beklenmeyen hata: {ex.Message}");
            }
        }

        public static BaseResponse AdminSignUpOperation(RegisterAdminRequest request, string token, UsersContext dbctx)
        {
            BaseResponse response = new();

            try
            {
                var currentSession = SessionService.TestToken(token, dbctx);
                if (currentSession == null)
                {
                    return response.GenerateError(1000, "Token geçersiz veya oturum süresi dolmuş.");
                }
                if (!SessionService.isAuthorized(currentSession, UserRoleinAuthorization.AdminAdmin))
                {
                     return response.GenerateError(1001, "Bu işlemi gerçekleştirme yetkiniz yok. Sadece AdminAdmin yapabilir.");
                }

                if (dbctx.AllUsersModel.Any(u => u.UserName == request.Username))
                {
                    return response.GenerateError(1006, "Bu kullanıcı adı zaten alınmış.");
                }

                var userToHash = new AllUsersModels { UserName = request.Username };
                var hashedPassword = _passwordHasher.HashPassword(userToHash, request.Password);

                var newAdmin = new AllUsersModels
                {
                    UserName = request.Username,
                    PasswordHash = hashedPassword,
                    UserImageLink = request.UserImageLink,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    UserRoleinAuthorization = UserRoleinAuthorization.Admin,
                    State = true,
                    ModTime = DateTime.UtcNow,
                    ModUser = currentSession._user.Id
                };

                dbctx.AllUsersModel.Add(newAdmin);
                dbctx.SaveChanges();

                response.Response = new
                {
                    UserId = newAdmin.Id,
                    UserName = newAdmin.UserName,
                    Role = newAdmin.UserRoleinAuthorization.ToString()
                };

                response.SetUserID(newAdmin.Id);
                return response.GenerateSuccess("Admin kullanıcı başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                return response.GenerateError(2003, $"Beklenmeyen hata: {ex.Message}");
            }
        }

    }
}
