using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using KeremProject1backend.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace KeremProject1backend.Operations
{
    public class TestOperations
    {
        private static readonly PasswordHasher<AllUsersModels> _passwordHasher = new PasswordHasher<AllUsersModels>();

        public static BaseResponse TestAuth(string token, UsersContext usersContext)
        {
            BaseResponse response = new();
            try
            {
                var session = SessionService.TestToken(token, usersContext);

                if (session == null)
                {
                    response.GenerateError(1000, "Token geçersiz veya oturum süresi dolmuş.");
                }
                else
                {
                    var testResponse = new TestAuthResponse
                    {
                        UserId = session._user.Id,
                        Role = session._user.UserRoleinAuthorization.ToString()
                    };
                    response.Response = testResponse;
                    response.GenerateSuccess("Token geçerli.");
                }
            }
            catch (Exception ex)
            {
                response.GenerateError(5000, $"TestAuth sırasında hata: {ex.Message}");
            }
            return response;
        }

        public static BaseResponse TestAdmin(string token, UsersContext usersContext)
        {
            BaseResponse response = new();
            try
            {
                var session = SessionService.TestToken(token, usersContext);
                if (session == null)
                {
                    response.GenerateError(1000, "Token geçersiz veya oturum süresi dolmuş.");
                }
                else if (session._user.UserRoleinAuthorization <= UserRoleinAuthorization.Admin)
                {
                    response.GenerateSuccess("Admin Yetkisi Doğrulandı.");
                }
                else
                {
                    response.GenerateError(1001, "Yetkisiz işlem. Admin yetkisi gerekli.");
                }
            }
            catch (Exception ex)
            {
                response.GenerateError(5001, $"TestAdmin sırasında hata: {ex.Message}");
            }
            return response;
        }

        public static BaseResponse TestOperation(TestRequest request, string Token, TestContext dbctx, UsersContext usersContext)
        {
            BaseResponse response = new();
            try
            {
                var currentsession = SessionService.TestToken(Token, usersContext);
                if (currentsession == null)
                {
                    return response.GenerateError(1000, "No Session");
                }
                var userid = response.SetUserID(currentsession._user.Id);
                if (!SessionService.isAuthorized(currentsession,UserRoleinAuthorization.AdminAdmin))
                {
                    return response.GenerateError(1001, "UnAuthorized Operation");
                }

                Test? resp = new Test();
                resp = dbctx.Test
                    .AsNoTracking()
                    .Where(row => row.Id == request.Id)
                    .FirstOrDefault();

                if (resp == null)
                    return response.GenerateError(1401, "No Record for this AVWorkNo : " + request.Id);

                response.Response = resp;
                return response.GenerateSuccess();
            }
            catch (Exception ex)
            {
                return response.GenerateError(1402, ex.ToString());
            }
        }

        public static async Task<BaseResponse> CreateInitialAdminOperation(RegisterAdminRequest request, UsersContext dbContext, ILogger logger)
        {
            var response = new BaseResponse();
            try
            {
                bool adminAdminExists = await dbContext.AllUsersModel.AnyAsync(u => u.UserRoleinAuthorization == UserRoleinAuthorization.AdminAdmin);
                if (adminAdminExists)
                {
                    logger?.LogWarning("CreateInitialAdminOperation denemesi: Zaten bir AdminAdmin kullanıcısı mevcut.");
                    response.GenerateError(1100, "İlk yönetici (AdminAdmin) zaten oluşturulmuş.");
                    return response;
                }

                bool usernameExists = await dbContext.AllUsersModel.AnyAsync(u => u.UserName == request.Username);
                if (usernameExists)
                {
                    response.GenerateError(1101, "Bu kullanıcı adı zaten kullanılıyor.");
                    return response;
                }

                logger?.LogInformation($"İlk AdminAdmin kullanıcısı oluşturuluyor: KullanıcıAdı='{request.Username}'");

                var userToHash = new AllUsersModels { UserName = request.Username };
                var hashedPassword = _passwordHasher.HashPassword(userToHash, request.Password);

                var newAdminAdmin = new AllUsersModels
                {
                    UserName = request.Username,
                    PasswordHash = hashedPassword,
                    UserRoleinAuthorization = UserRoleinAuthorization.AdminAdmin,
                    State = true,
                    UserImageLink = request.UserImageLink,
                    ModTime = DateTime.UtcNow,
                    ModUser = 0
                };

                dbContext.AllUsersModel.Add(newAdminAdmin);
                await dbContext.SaveChangesAsync();

                logger?.LogInformation($"İlk AdminAdmin kullanıcısı başarıyla oluşturuldu: ID={newAdminAdmin.Id}, KullanıcıAdı='{newAdminAdmin.UserName}'");

                response.Response = new { UserId = newAdminAdmin.Id, UserName = newAdminAdmin.UserName };
                response.GenerateSuccess("İlk yönetici (AdminAdmin) başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"CreateInitialAdminOperation sırasında hata oluştu. Kullanıcı Adı: '{request.Username}'");
                response.GenerateError(5900, $"İlk yönetici oluşturulurken hata: {ex.Message}");
            }
            return response;
        }
    }
}
