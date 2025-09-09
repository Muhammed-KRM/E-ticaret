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
    public static class CategoryOperations
    {
        // Admin yetkisi kontrolü
        private static bool IsAdmin(string token, UsersContext usersContext)
        {
            var session = SessionService.TestToken(token, usersContext);
            return session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.AdminAdmin ||
                   session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.Admin;
        }

        // Yeni Kategori Ekleme
        public static async Task<BaseResponse> CreateCategoryOperation(string token, CreateCategoryRequest request, GeneralContext dbContext, UsersContext usersContext, ILogger logger)
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

                // Aynı isimde başka kategori var mı?
                bool exists = await dbContext.Categories.AnyAsync(c => c.Name == request.Name);
                if (exists)
                {
                    response.GenerateError(4601, "Bu isimde bir kategori zaten mevcut.");
                    return response;
                }

                var newCategory = new Category
                {
                    Name = request.Name,
                    Description = request.Description
                };

                dbContext.Categories.Add(newCategory);
                await dbContext.SaveChangesAsync();
                logger?.LogInformation($"Yeni kategori eklendi: ID={newCategory.Id}, Ad={newCategory.Name}");

                // Log kayıt işlemi
                await LogServices.AddLogAsync(
                    dbContext, 
                    "Categories", 
                    'C', // Create
                    userId,
                    null, // Önceki değer yok
                    newCategory // Yeni eklenen kategori
                );

                response.Response = new { CategoryId = newCategory.Id };
                response.GenerateSuccess("Kategori başarıyla eklendi.");
            }
            catch (Exception ex)
            {
                 logger?.LogError(ex, "CreateCategoryOperation sırasında hata oluştu.");
                 response.GenerateError(5600, $"Kategori eklenirken hata: {ex.Message}");
            }
            return response;
        }

        // Tüm Kategorileri Listeleme
        public static async Task<BaseResponse> GetAllCategoriesOperation(GeneralContext dbContext, ILogger logger)
        {
             var response = new BaseResponse();
            try
            {
                var categories = await dbContext.Categories
                                        .OrderBy(c => c.Name)
                                        .Select(c => new CategoryResponse
                                        {
                                            Id = c.Id,
                                            Name = c.Name,
                                            Description = c.Description
                                            // ProductCount = c.Products.Count() // Ürün sayısını da istersen
                                        })
                                        .ToListAsync();

                response.Response = categories;
                response.GenerateSuccess("Kategoriler başarıyla listelendi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "GetAllCategoriesOperation sırasında hata oluştu.");
                response.GenerateError(5601, $"Kategoriler listelenirken hata: {ex.Message}");
            }
            return response;
        }

        // Kategori Güncelleme
        public static async Task<BaseResponse> UpdateCategoryOperation(string token, int categoryId, UpdateCategoryRequest request, GeneralContext dbContext, UsersContext usersContext, ILogger logger)
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

                if (categoryId != request.Id)
                {
                     response.GenerateError(4000, "URL'deki kategori ID'si ile istekteki ID uyuşmuyor.");
                     return response;
                }

                var category = await dbContext.Categories.FindAsync(categoryId);
                if (category == null)
                {
                    response.GenerateError(4404, "Güncellenecek kategori bulunamadı.");
                    return response;
                }

                // Değişiklik öncesi kategori değerlerini sakla (log için)
                var oldCategory = new Category
                {
                    Id = category.Id,
                    Name = category.Name, 
                    Description = category.Description
                };

                // Başka bir kategoride aynı isim var mı kontrolü (kendisi hariç)
                bool nameExists = await dbContext.Categories.AnyAsync(c => c.Id != categoryId && c.Name == request.Name);
                if (nameExists)
                {
                    response.GenerateError(4601, "Bu isimde başka bir kategori zaten mevcut.");
                    return response;
                }

                category.Name = request.Name;
                category.Description = request.Description;

                dbContext.Entry(category).State = EntityState.Modified;
                await dbContext.SaveChangesAsync();
                logger?.LogInformation($"Kategori güncellendi: ID={category.Id}, Ad={category.Name}");

                // Güncelleme logunu kaydet
                await LogServices.AddLogAsync(
                    dbContext,
                    "Categories",
                    'U', // Update
                    userId,
                    oldCategory, // Önceki değer
                    category // Yeni değer
                );

                response.GenerateSuccess("Kategori başarıyla güncellendi.");
            }
             catch (DbUpdateConcurrencyException ex)
            {
                logger?.LogWarning(ex, $"UpdateCategoryOperation - Concurrency hatası (ID: {categoryId}).");
                response.GenerateError(4602, "Kategori güncellenirken bir çakışma oluştu. Lütfen tekrar deneyin.");
            }
            catch (Exception ex)
            {
                 logger?.LogError(ex, $"UpdateCategoryOperation sırasında hata oluştu (ID: {categoryId}).");
                 response.GenerateError(5602, $"Kategori güncellenirken hata: {ex.Message}");
            }
            return response;
        }

        // Kategori Silme
        public static async Task<BaseResponse> DeleteCategoryOperation(string token, int categoryId, GeneralContext dbContext, UsersContext usersContext, ILogger logger)
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

                var category = await dbContext.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == categoryId);
                if (category == null)
                {
                    response.GenerateError(4404, "Silinecek kategori bulunamadı.");
                    return response;
                }

                // Silme öncesi kategori bilgilerini sakla (log için)
                var oldCategory = new Category
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description
                };

                // Kategoride ürün varsa silmeyi engelle veya ürünlerin kategorisini null yap
                // Bizim OnDelete(DeleteBehavior.SetNull) ayarımız ürünlerin kategorisini null yapacak.
                // if (category.Products.Any())
                // {
                //     response.GenerateError(4603, "Bu kategoride ürünler bulunduğu için silinemez. Önce ürünleri başka bir kategoriye taşıyın veya silin.");
                //     return response;
                // }

                dbContext.Categories.Remove(category);
                await dbContext.SaveChangesAsync();
                logger?.LogInformation($"Kategori silindi: ID={categoryId}, Ad={category.Name}. İlişkili ürünlerin CategoryId'si NULL yapıldı.");

                // Silme logunu kaydet
                await LogServices.AddLogAsync(
                    dbContext,
                    "Categories",
                    'D', // Delete
                    userId,
                    oldCategory, // Silinen kategori bilgileri
                    null // Yeni değer yok
                );

                response.GenerateSuccess("Kategori başarıyla silindi.");
            }
            catch (Exception ex)
            {
                 logger?.LogError(ex, $"DeleteCategoryOperation sırasında hata oluştu (ID: {categoryId}).");
                 response.GenerateError(5603, $"Kategori silinirken hata: {ex.Message}");
            }
            return response;
        }
    }
} 