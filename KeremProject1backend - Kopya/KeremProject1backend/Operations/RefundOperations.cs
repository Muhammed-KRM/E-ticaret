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
    public static class RefundOperations
    {
        // Admin yetkisi kontrolü (ProductOperations'dan alınabilir veya ortak bir yere taşınabilir)
        private static bool IsAdmin(string token, UsersContext usersContext)
        {
            var session = SessionService.TestToken(token, usersContext);
            return session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.AdminAdmin ||
                   session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.Admin;
        }

        public static async Task<BaseResponse> RequestRefundOperation(string token, RefundRequest request, GeneralContext dbContext, PayTrService payTrService, UsersContext usersContext, ILogger logger)
        {
            var response = new BaseResponse();
            try
            {
                // 1. Yetki Kontrolü
                if (!IsAdmin(token, usersContext))
                {
                    response.GenerateError(1001, "Yetkisiz işlem. Admin girişi gereklidir.");
                    return response;
                }

                // 2. İlgili Siparişi Bul
                var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.MerchantOid == request.MerchantOid);

                if (order == null)
                {
                    response.GenerateError(4404, "İade edilecek sipariş bulunamadı.");
                    return response;
                }

                // 3. Sipariş Durumunu Kontrol Et (İade edilebilir mi?)
                // Örneğin, sadece 'Paid', 'Processing', 'Shipped', 'Delivered' durumundakiler iade edilebilir.
                if (order.Status != OrderStatus.Paid &&
                    order.Status != OrderStatus.Processing &&
                    order.Status != OrderStatus.Shipped &&
                    order.Status != OrderStatus.Delivered)
                {
                    response.GenerateError(4301, $"Bu sipariş durumu ({order.Status}) iade için uygun değil.");
                    return response;
                }

                // 4. İade Tutarını Kontrol Et (Sipariş tutarından fazla olamaz)
                if (request.RefundAmount > order.TotalAmount)
                {
                    response.GenerateError(4302, $"İade tutarı ({request.RefundAmount}), sipariş tutarından ({order.TotalAmount}) fazla olamaz.");
                    return response;
                }
                 // TODO: Kısmi iade yapıldıysa, kalan tutardan fazla iade yapılamaz kontrolü eklenebilir.

                // 5. PayTR Servisi ile İade İsteği Gönder
                logger?.LogInformation($"İade işlemi başlatılıyor: OID={request.MerchantOid}, Tutar={request.RefundAmount}");
                var (refundSuccess, refundMessage) = await payTrService.SendRefundRequestAsync(request.MerchantOid, request.RefundAmount);

                if (!refundSuccess)
                {
                    // PayTR hatasını logladık, kullanıcıya daha genel bir mesaj dönebiliriz.
                     response.GenerateError(5300, $"PayTR iade işlemi başarısız: {refundMessage}");
                     return response;
                }

                // 6. İade Başarılıysa Sipariş Durumunu Güncelle (veya yeni bir iade kaydı oluştur)
                // Şimdilik sipariş durumunu 'Refunded' yapalım.
                // Daha karmaşık senaryolarda (kısmi iade vb.) ayrı bir 'Refunds' tablosu tutulabilir.
                order.Status = OrderStatus.Refunded; // Yeni bir durum eklemek gerekebilir: PartiallyRefunded?
                // order.RefundedAmount += request.RefundAmount; // İade edilen toplam tutarı takip etmek için
                dbContext.Entry(order).State = EntityState.Modified;
                await dbContext.SaveChangesAsync();

                logger?.LogInformation($"İade işlemi başarılı ve sipariş durumu güncellendi: OID={request.MerchantOid}, Durum={order.Status}");
                response.GenerateSuccess($"İade talebi başarıyla işlendi. ({refundMessage})");

            }
            catch (Exception ex)
            {
                 logger?.LogError(ex, $"RequestRefundOperation sırasında hata oluştu: OID={request.MerchantOid}");
                 response.GenerateError(5301, $"İade işlemi sırasında beklenmeyen bir hata oluştu: {ex.Message}");
            }
            return response;
        }

        // Onay Bekleyen İade Taleplerini Listeleme
        public static async Task<BaseResponse> GetPendingReturnRequestsOperation(string token, GeneralContext dbContext, UsersContext usersContext, ILogger logger)
        {
            var response = new BaseResponse();
            try
            {
                if (!IsAdmin(token, usersContext))
                {
                    response.GenerateError(1001, "Yetkisiz işlem. Admin girişi gereklidir.");
                    return response;
                }

                var pendingReturns = await dbContext.Orders
                                            .Where(o => o.Status == OrderStatus.ReturnRequested)
                                            .OrderByDescending(o => o.OrderDate) // Veya iade talep tarihine göre
                                            .Select(o => new ReturnRequestSummaryResponse
                                            {
                                                OrderId = o.Id,
                                                MerchantOid = o.MerchantOid,
                                                OrderDate = o.OrderDate,
                                                TotalAmount = o.TotalAmount,
                                                UserId = o.UserId,
                                                ReturnReason = o.ReturnReason,
                                                CurrentStatus = o.Status.ToString()
                                            })
                                            .ToListAsync();

                response.Response = pendingReturns;
                response.GenerateSuccess("Onay bekleyen iade talepleri başarıyla listelendi.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "GetPendingReturnRequestsOperation sırasında hata oluştu.");
                response.GenerateError(5600, $"İade talepleri listelenirken hata: {ex.Message}");
            }
            return response;
        }

        // İade Talebini Onaylama
        public static async Task<BaseResponse> ApproveReturnRequestOperation(string token, int orderId, ProcessReturnRequest request, GeneralContext dbContext, PayTrService payTrService, UsersContext usersContext, ILogger logger)
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

                var order = await dbContext.Orders.FindAsync(orderId);
                if (order == null)
                {
                    response.GenerateError(4404, "İşlem yapılacak sipariş bulunamadı.");
                    return response;
                }

                if (order.Status != OrderStatus.ReturnRequested)
                {
                    response.GenerateError(4304, "Bu sipariş için onay bekleyen bir iade talebi yok.");
                    return response;
                }

                // Değişiklik öncesi durumu sakla (log için)
                var oldOrder = new Order
                {
                    Id = order.Id,
                    MerchantOid = order.MerchantOid,
                    UserId = order.UserId,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    ReturnReason = order.ReturnReason
                };

                // İade tutarını belirle (istekte belirtilmişse onu, yoksa tam tutarı)
                decimal amountToRefund = request.RefundAmount ?? order.TotalAmount;

                // Tekrar kontrol: Belirlenen iade tutarı sipariş tutarından fazla olamaz
                if (amountToRefund > order.TotalAmount || amountToRefund <= 0)
                {
                    response.GenerateError(4302, $"Geçersiz iade tutarı: {amountToRefund}. Tutar 0'dan büyük ve sipariş tutarından ({order.TotalAmount}) küçük veya eşit olmalıdır.");
                    return response;
                }

                // PayTR Üzerinden Para İadesini Gerçekleştir
                logger?.LogInformation($"İade talebi onaylanıyor, PayTR iadesi başlatılıyor: OID={order.MerchantOid}, Tutar={amountToRefund}");
                var (refundSuccess, refundMessage) = await payTrService.SendRefundRequestAsync(order.MerchantOid, amountToRefund);

                if (!refundSuccess)
                {
                    // PayTR hatası durumunda işlemi durdur ve hata dön
                    response.GenerateError(5300, $"PayTR iade işlemi başarısız oldu: {refundMessage}");
                    return response;
                }

                // PayTR iadesi başarılıysa sipariş durumunu güncelle
                order.Status = amountToRefund == order.TotalAmount ? OrderStatus.Refunded : OrderStatus.PartiallyRefunded;
                // order.AdminNotes = request.AdminNotes; // Admin notunu kaydetmek için Order'a alan eklenebilir
                // order.RefundedAmount += amountToRefund; // Toplam iadeyi takip etmek için
                dbContext.Entry(order).State = EntityState.Modified;
                await dbContext.SaveChangesAsync();

                // İade onaylama logunu kaydet
                await LogServices.AddLogAsync(
                    dbContext,
                    "Orders",
                    'U', // Update
                    userId,
                    oldOrder, // Önceki değer
                    order // Yeni değer (iade onaylanmış)
                );

                logger?.LogInformation($"İade talebi onaylandı ve PayTR iadesi başarılı: OID={order.MerchantOid}, Durum={order.Status}");
                response.GenerateSuccess($"İade talebi başarıyla onaylandı ve ödeme iadesi yapıldı. ({refundMessage})");

                // TODO: Stokları geri ekle (eğer ürünler fiziksel olarak geri alındıysa)
                // TODO: Kullanıcıya iadenin onaylandığına dair bildirim gönder.
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"ApproveReturnRequestOperation sırasında hata oluştu: OrderId={orderId}");
                response.GenerateError(5601, $"İade talebi onaylanırken hata: {ex.Message}");
            }
            return response;
        }

        // İade Talebini Reddetme
        public static async Task<BaseResponse> RejectReturnRequestOperation(string token, int orderId, ProcessReturnRequest request, GeneralContext dbContext, UsersContext usersContext, ILogger logger)
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

                var order = await dbContext.Orders.FindAsync(orderId);
                if (order == null)
                {
                    response.GenerateError(4404, "İşlem yapılacak sipariş bulunamadı.");
                    return response;
                }

                if (order.Status != OrderStatus.ReturnRequested)
                {
                    response.GenerateError(4304, "Bu sipariş için onay bekleyen bir iade talebi yok.");
                    return response;
                }

                // Değişiklik öncesi durumu sakla (log için)
                var oldOrder = new Order
                {
                    Id = order.Id,
                    MerchantOid = order.MerchantOid,
                    UserId = order.UserId,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    ReturnReason = order.ReturnReason
                };

                // Sipariş durumunu Reddedildi yap
                order.Status = OrderStatus.ReturnRejected;
                // order.AdminNotes = request.AdminNotes; // Admin notunu kaydetmek için Order'a alan eklenebilir
                dbContext.Entry(order).State = EntityState.Modified;
                await dbContext.SaveChangesAsync();

                // İade reddetme logunu kaydet
                await LogServices.AddLogAsync(
                    dbContext,
                    "Orders",
                    'U', // Update
                    userId,
                    oldOrder, // Önceki değer
                    order // Yeni değer (iade reddedilmiş)
                );

                logger?.LogInformation($"İade talebi reddedildi: OID={order.MerchantOid}");
                response.GenerateSuccess("İade talebi başarıyla reddedildi.");

                // TODO: Kullanıcıya iadenin reddedildiğine dair bildirim gönder (sebep belirterek).
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"RejectReturnRequestOperation sırasında hata oluştu: OrderId={orderId}");
                response.GenerateError(5602, $"İade talebi reddedilirken hata: {ex.Message}");
            }
            return response;
        }

    }
} 