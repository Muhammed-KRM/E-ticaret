using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using KeremProject1backend.Services; // SessionService için

namespace KeremProject1backend.Operations
{
    public static class ReviewOperations
    {
        // Admin yetkisi kontrolü (ProductOperations'dan kopyalandı)
        private static bool IsAdmin(string token, UsersContext usersContext)
        {
            var session = SessionService.TestToken(token, usersContext);
            return session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.AdminAdmin ||
                   session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.Admin;
        }

        public static async Task<BaseResponse> AddReviewOperation(string token, AddReviewRequest request, GeneralContext dbContext, UsersContext usersContext, ILogger logger)
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

                // Ürünün varlığını kontrol et
                bool productExists = await dbContext.Products.AnyAsync(p => p.Id == request.ProductId && p.IsActive);
                if (!productExists)
                {
                    response.GenerateError(4404, "Yorum yapılacak ürün bulunamadı veya aktif değil.");
                    return response;
                }

                // Kullanıcının bu ürüne daha önce yorum yapıp yapmadığını kontrol et
                bool hasReviewed = await dbContext.ProductReviews
                                            .AnyAsync(r => r.UserId == userIdInt && r.ProductId == request.ProductId);
                if (hasReviewed)
                {
                    response.GenerateError(4502, "Bu ürüne zaten yorum yaptınız.");
                    return response;
                }

                var review = new ProductReview
                {
                    ProductId = request.ProductId,
                    UserId = userIdInt,
                    UserName = currentSession._user.UserName,
                    Rating = request.Rating,
                    Comment = request.Comment,
                    ReviewDate = DateTime.UtcNow,
                    IsApproved = true
                };

                dbContext.ProductReviews.Add(review);
                await dbContext.SaveChangesAsync();
                logger?.LogInformation($"Yeni ürün yorumu eklendi: ProductId={request.ProductId}, UserId={userIdInt}");

                // TODO: Ürünün ortalama puanını güncelle (Products tablosunda bir alan varsa)

                response.GenerateSuccess("Yorumunuz başarıyla eklendi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "AddReviewOperation sırasında hata oluştu.");
                response.GenerateError(5500, $"Yorum eklenirken hata: {ex.Message}");
            }
            return response;
        }

        public static async Task<BaseResponse> GetProductReviewsOperation(int productId, GeneralContext dbContext, ILogger logger)
        {
            var response = new BaseResponse();
            try
            {
                var reviews = await dbContext.ProductReviews
                                        .Where(r => r.ProductId == productId)
                                        .OrderByDescending(r => r.ReviewDate)
                                        .Select(r => new ProductReviewResponse
                                        {
                                            Id = r.Id,
                                            UserId = r.UserId,
                                            UserName = r.UserName,
                                            Rating = r.Rating,
                                            Comment = r.Comment,
                                            ReviewDate = r.ReviewDate
                                        })
                                        .ToListAsync();

                response.Response = reviews;
                response.GenerateSuccess("Ürün yorumları başarıyla getirildi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"GetProductReviewsOperation sırasında hata oluştu: ProductId={productId}");
                response.GenerateError(5501, $"Yorumlar getirilirken hata: {ex.Message}");
            }
            return response;
        }

        // Yeni: Yorum Silme Operasyonu (Admin)
        public static async Task<BaseResponse> DeleteReviewOperation(string token, int reviewId, GeneralContext dbContext, UsersContext usersContext, ILogger logger)
        {
            var response = new BaseResponse();
            try
            {
                if (!IsAdmin(token, usersContext))
                {
                    response.GenerateError(1001, "Yetkisiz işlem. Admin girişi gereklidir.");
                    return response;
                }

                var review = await dbContext.ProductReviews.FindAsync(reviewId);

                if (review == null)
                {
                    response.GenerateError(4404, "Silinecek yorum bulunamadı.");
                    return response;
                }

                dbContext.ProductReviews.Remove(review);
                await dbContext.SaveChangesAsync();
                logger?.LogInformation($"Yorum silindi: ReviewId={reviewId}");

                response.GenerateSuccess("Yorum başarıyla silindi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"DeleteReviewOperation sırasında hata oluştu: ReviewId={reviewId}");
                response.GenerateError(5502, $"Yorum silinirken hata: {ex.Message}");
            }
            return response;
        }
    }
} 