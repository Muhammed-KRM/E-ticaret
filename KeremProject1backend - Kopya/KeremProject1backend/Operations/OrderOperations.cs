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
    public static class OrderOperations
    {
        public static async Task<BaseResponse> GetUserOrdersOperation(string token, GeneralContext dbContext, UsersContext usersContext)
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

                var orders = await dbContext.Orders
                                        .Where(o => o.UserId == userIdInt)
                                        .OrderByDescending(o => o.OrderDate)
                                        .Select(o => new OrderSummaryResponse
                                        {
                                            OrderId = o.Id,
                                            MerchantOid = o.MerchantOid,
                                            OrderDate = o.OrderDate,
                                            TotalAmount = o.TotalAmount,
                                            Status = o.Status.ToString() // Enum'ı string'e çevir
                                        })
                                        .ToListAsync();

                response.Response = orders;
                response.GenerateSuccess("Siparişler başarıyla getirildi.");
                return response;
            }
            catch (Exception ex)
            {
                response.GenerateError(5200, $"Siparişler getirilirken hata: {ex.Message}");
                return response;
            }
        }

        public static async Task<BaseResponse> GetOrderDetailsOperation(string token, int orderId, GeneralContext dbContext, UsersContext usersContext)
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

                var orderDetails = await dbContext.Orders
                                                .Include(o => o.OrderItems) // Sipariş öğelerini dahil et
                                                .Where(o => o.Id == orderId && o.UserId == userIdInt)
                                                .Select(o => new OrderDetailsResponse
                                                {
                                                    OrderId = o.Id,
                                                    MerchantOid = o.MerchantOid,
                                                    OrderDate = o.OrderDate,
                                                    TotalAmount = o.TotalAmount,
                                                    Status = o.Status.ToString(),
                                                    ShippingAddress = o.ShippingAddress,
                                                    BillingAddress = o.BillingAddress,
                                                    TrackingNumber = o.TrackingNumber,
                                                    ShippingCarrier = o.ShippingCarrier,
                                                    ShippedDate = o.ShippedDate,
                                                    DeliveredDate = o.DeliveredDate,
                                                    Items = o.OrderItems.Select(oi => new OrderItemResponse
                                                    {
                                                        ProductId = oi.ProductId,
                                                        ProductName = oi.ProductName,
                                                        Quantity = oi.Quantity,
                                                        UnitPrice = oi.UnitPrice
                                                    }).ToList()
                                                })
                                                .FirstOrDefaultAsync();

                if (orderDetails == null)
                {
                    response.GenerateError(4201, "Sipariş bulunamadı veya yetkiniz yok.");
                    return response;
                }

                response.Response = orderDetails;
                response.GenerateSuccess("Sipariş detayı başarıyla getirildi.");
                return response;
            }
            catch (Exception ex)
            {
                response.GenerateError(5201, $"Sipariş detayı getirilirken hata: {ex.Message}");
                return response;
            }
        }

        public static async Task<BaseResponse> CancelOrderOperation(string token, int orderId, CancelOrderRequest request, GeneralContext dbContext, UsersContext usersContext, PayTrService payTrService, ILogger logger)
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

                var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userIdInt);

                if (order == null)
                {
                    response.GenerateError(4201, "Sipariş bulunamadı veya yetkiniz yok.");
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
                    ShippingAddress = order.ShippingAddress,
                    BillingAddress = order.BillingAddress,
                    TrackingNumber = order.TrackingNumber,
                    ShippingCarrier = order.ShippingCarrier
                };

                // İptal koşullarını kontrol et (örn. sadece Pending veya Processing durumundaysa)
                if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing && order.Status != OrderStatus.Paid /* Ödeme yapıldıysa iade gerekebilir */)
                {
                    response.GenerateError(4202, $"Bu sipariş durumu iptal edilemez: {order.Status}");
                    return response;
                }

                bool refundRequired = order.Status == OrderStatus.Paid; // Ödeme yapıldıysa iade gerekir
                bool localRefundSuccessFlag = !refundRequired; // İade gerekmiyorsa başarılı varsay
                string refundMessage = string.Empty;

                if (refundRequired)
                {
                    logger?.LogInformation($"İptal için PayTR iade isteği gönderiliyor: OID={order.MerchantOid}");
                    // PayTrService üzerinden iade isteği gönder
                    var (paytrSuccess, msg) = await payTrService.SendRefundRequestAsync(order.MerchantOid, order.TotalAmount);
                    localRefundSuccessFlag = paytrSuccess;
                    refundMessage = msg;
                }

                if (localRefundSuccessFlag)
                {
                    order.Status = OrderStatus.Cancelled;
                    order.CancellationReason = request.Reason;
                    await dbContext.SaveChangesAsync();
                    logger?.LogInformation($"Sipariş iptal edildi: OID={order.MerchantOid}, İade Yapıldı: {refundRequired}, Mesaj: {refundMessage}");

                    // İptal logunu kaydet
                    await LogServices.AddLogAsync(
                        dbContext,
                        "Orders",
                        'U', // Update (durumun değiştirilmesi)
                        userIdInt,
                        oldOrder, // Önceki değer
                        order // Yeni değer (iptal edilmiş)
                    );

                    // TODO: İptal sonrası işlemler:
                    // - Stokları geri ekle (eğer düşüldüyse)
                    // - Kullanıcıya bildirim gönder

                    response.GenerateSuccess("Sipariş başarıyla iptal edildi.");
                    return response;
                }
                else
                {
                    logger?.LogError($"PayTR iade isteği başarısız oldu! Sipariş iptal edilemedi. OID: {order.MerchantOid}, Hata: {refundMessage}");
                    response.GenerateError(4203, $"Ödeme iadesi yapılamadığı için sipariş iptal edilemedi: {refundMessage}");
                    return response;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "CancelOrderOperation sırasında hata oluştu.");
                response.GenerateError(5202, $"Sipariş iptal edilirken hata: {ex.Message}");
                return response;
            }
        }

        public static async Task<BaseResponse> RequestReturnOperation(string token, int orderId, ReturnRequest request, GeneralContext dbContext, UsersContext usersContext, ILogger logger)
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

                var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userIdInt);

                if (order == null)
                {
                    response.GenerateError(4201, "Sipariş bulunamadı veya yetkiniz yok.");
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
                    ShippingAddress = order.ShippingAddress,
                    BillingAddress = order.BillingAddress,
                    ReturnReason = order.ReturnReason
                };

                // İade koşullarını kontrol et (örn. sadece Delivered durumundaysa ve belirli bir süre geçmediyse)
                if (order.Status != OrderStatus.Delivered)
                {
                    // Belki Shipped durumunda da iade talebine izin verilebilir?
                    response.GenerateError(4301, $"Bu sipariş durumu ({order.Status}) için iade talebi oluşturulamaz.");
                    return response;
                }
                // İade süresi kontrolü (Örnek: Teslimattan sonra 14 gün)
                if (order.DeliveredDate.HasValue && (DateTime.UtcNow - order.DeliveredDate.Value).TotalDays > 14)
                {
                    response.GenerateError(4302, "Yasal iade süresi (14 gün) dolmuştur.");
                    return response;
                }
                // Zaten iade talebi var mı kontrolü
                if (order.Status == OrderStatus.ReturnRequested || order.Status == OrderStatus.Refunded || order.Status == OrderStatus.PartiallyRefunded || order.Status == OrderStatus.ReturnRejected)
                {
                    response.GenerateError(4303, "Bu sipariş için zaten bir iade süreci başlatılmış veya sonuçlanmış.");
                    return response;
                }

                // İade talebini kaydet
                order.Status = OrderStatus.ReturnRequested; // Durumu "İade Talep Edildi" yap
                order.ReturnReason = request.Reason;
                // İsteğe bağlı: Talep tarihini kaydetmek için yeni bir alan eklenebilir (ReturnRequestDate)
                // order.ReturnRequestDate = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();
                logger?.LogInformation($"İade talebi alındı: OID={order.MerchantOid}, UserID={userIdInt}");

                // İade talebi logunu kaydet
                await LogServices.AddLogAsync(
                    dbContext,
                    "Orders",
                    'U', // Update (durumun değiştirilmesi)
                    userIdInt,
                    oldOrder, // Önceki değer
                    order // Yeni değer (iade talep edilmiş)
                );

                // TODO:
                // - Kullanıcıya "iade talebiniz alındı, inceleniyor" bildirimi gönder.
                // - Admin paneline bu talebi bildir.

                response.GenerateSuccess("İade talebiniz alınmıştır ve incelenmektedir.");
                return response;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "RequestReturnOperation sırasında hata oluştu.");
                response.GenerateError(5300, $"İade talebi oluşturulurken hata: {ex.Message}");
                return response;
            }
        }

        // Admin için Kargo Bilgisi Güncelleme
        public static async Task<BaseResponse> UpdateShippingInfoOperation(string token, int orderId, UpdateShippingInfoRequest request, GeneralContext dbContext, UsersContext usersContext, ILogger logger, EmailService emailService, string? callbackBaseUrl = null)
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
                if(order == null)
                {
                    response.GenerateError(4201, "Sipariş bulunamadı.");
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
                    ShippingAddress = order.ShippingAddress,
                    BillingAddress = order.BillingAddress,
                    TrackingNumber = order.TrackingNumber,
                    ShippingCarrier = order.ShippingCarrier,
                    ShippedDate = order.ShippedDate
                };

                order.TrackingNumber = request.TrackingNumber;
                order.ShippingCarrier = request.ShippingCarrier;
                order.Status = OrderStatus.Shipped;
                order.ShippedDate = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();
                logger?.LogInformation($"Kargo bilgisi güncellendi: OID={order.MerchantOid}, KargoNo={order.TrackingNumber}");

                // Güncelleme logunu kaydet
                await LogServices.AddLogAsync(
                    dbContext,
                    "Orders",
                    'U', // Update
                    userId,
                    oldOrder, // Önceki değer
                    order // Yeni değer
                );

                // Kullanıcıya kargo bildirimi gönder
                try
                {
                    if (!string.IsNullOrEmpty(order.CustomerEmail))
                    {
                        string? orderDetailsUrl = !string.IsNullOrEmpty(callbackBaseUrl) ? $"{callbackBaseUrl}/orders/{order.Id}" : null;
                        string customerNameForEmail = !string.IsNullOrEmpty(order.CustomerName) ? order.CustomerName : "Değerli Müşterimiz";
                        await emailService.SendOrderStatusUpdateToUserAsync(order.CustomerEmail, customerNameForEmail, order.Id, "Shipped", order.TrackingNumber, order.ShippingCarrier, orderDetailsUrl);
                        logger?.LogInformation($"Kullanıcıya kargo bilgisi e-postası gönderildi: OrderID={order.Id}");
                    }
                    else
                    {
                        logger?.LogWarning($"Kullanıcı e-postası boş olduğu için kargo bildirimi gönderilemedi: OrderID={order.Id}");
                    }
                }
                catch (Exception emailEx)
                {
                    logger?.LogError(emailEx, $"Kargo bilgisi güncelleme sonrası e-posta gönderiminde hata: OrderID={order.Id}");
                }

                response.GenerateSuccess("Kargo bilgisi güncellendi.");
                return response;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "UpdateShippingInfoOperation sırasında hata oluştu.");
                response.GenerateError(5203, $"Kargo bilgisi güncellenirken hata: {ex.Message}");
                return response;
            }
        }

        // Yeni: Admin Sipariş Durumu Güncelleme
        public static async Task<BaseResponse> UpdateOrderStatusOperation(string token, int orderId, UpdateOrderStatusRequest request, GeneralContext dbContext, UsersContext usersContext, ILogger logger, EmailService emailService, string? callbackBaseUrl = null)
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
                    response.GenerateError(4201, "Sipariş bulunamadı.");
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
                    ShippingAddress = order.ShippingAddress,
                    BillingAddress = order.BillingAddress,
                    TrackingNumber = order.TrackingNumber,
                    ShippingCarrier = order.ShippingCarrier
                };

                order.Status = request.Status;
                await dbContext.SaveChangesAsync();
                logger?.LogInformation($"Sipariş durumu güncellendi: OID={order.MerchantOid}, Yeni Durum={request.Status}");

                // Güncelleme logunu kaydet
                await LogServices.AddLogAsync(
                    dbContext,
                    "Orders",
                    'U', // Update
                    userId,
                    oldOrder, // Önceki değer
                    order // Yeni değer
                );

                // Kullanıcıya durum güncelleme e-postası gönder
                try
                {
                    if (!string.IsNullOrEmpty(order.CustomerEmail))
                    {
                        string? orderDetailsUrl = !string.IsNullOrEmpty(callbackBaseUrl) ? $"{callbackBaseUrl}/orders/{order.Id}" : null;
                        string customerNameForEmail = !string.IsNullOrEmpty(order.CustomerName) ? order.CustomerName : "Değerli Müşterimiz";
                        await emailService.SendOrderStatusUpdateToUserAsync(order.CustomerEmail, customerNameForEmail, order.Id, request.Status.ToString(), order.TrackingNumber, order.ShippingCarrier, orderDetailsUrl);
                        logger?.LogInformation($"Kullanıcıya sipariş durumu ({request.Status}) güncelleme e-postası gönderildi: OrderID={order.Id}");
                    }
                    else
                    {
                        logger?.LogWarning($"Kullanıcı e-postası boş olduğu için sipariş durumu güncelleme e-postası gönderilemedi: OrderID={order.Id}");
                    }
                }
                catch (Exception emailEx)
                {
                    logger?.LogError(emailEx, $"Sipariş durumu güncelleme sonrası e-posta gönderiminde hata: OrderID={order.Id}");
                }

                response.GenerateSuccess("Sipariş durumu başarıyla güncellendi.");
                return response;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "UpdateOrderStatusOperation sırasında hata oluştu.");
                response.GenerateError(5204, $"Sipariş durumu güncellenirken hata: {ex.Message}");
                return response;
            }
        }

        // Kullanıcı ve Admin için Kargo Takip Bilgisi Alma
        public static async Task<BaseResponse> GetTrackingInfoOperation(string token, int orderId, GeneralContext dbContext, UsersContext usersContext, ShippingService shippingService, ILogger logger)
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
                bool isAdmin = currentSession._user.UserRoleinAuthorization <= UserRoleinAuthorization.Admin;

                // Admin değilse sadece kendi siparişini görebilir
                var query = dbContext.Orders.Include(o => o.OrderItems).AsQueryable();
                
                if (!isAdmin)
                {
                    query = query.Where(o => o.Id == orderId && o.UserId == userIdInt);
                }
                else
                {
                    query = query.Where(o => o.Id == orderId);
                }

                var order = await query
                    .Select(o => new OrderAdminResponse
                    {
                        OrderId = o.Id,
                        MerchantOid = o.MerchantOid,
                        UserId = o.UserId,
                        OrderDate = o.OrderDate,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status.ToString(),
                        ShippingAddress = o.ShippingAddress,
                        BillingAddress = o.BillingAddress,
                        TrackingNumber = o.TrackingNumber,
                        ShippingCarrier = o.ShippingCarrier,
                        ShippedDate = o.ShippedDate,
                        DeliveredDate = o.DeliveredDate,
                        CancellationReason = o.CancellationReason,
                        ReturnReason = o.ReturnReason,
                        CustomerName = o.CustomerName,
                        CustomerEmail = o.CustomerEmail,
                        Items = o.OrderItems.Select(oi => new OrderItemResponse
                        {
                            ProductId = oi.ProductId,
                            ProductName = oi.ProductName,
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    response.GenerateError(4201, "Sipariş bulunamadı veya yetkiniz yok.");
                    return response;
                }

                // Kullanıcı bilgilerini ekle
                if (order.UserId.HasValue && usersContext != null)
                {
                    var user = await usersContext.AllUsersModel.Where(u => u.Id == order.UserId.Value)
                        .Select(u => new { u.UserName, u.Email })
                        .FirstOrDefaultAsync();

                    if (user != null)
                    {
                        order.UserName = user.UserName;
                        order.UserEmail = user.Email;
                    }
                }
                else if (!order.UserId.HasValue)
                {
                    // Üye olmayan kullanıcılar için Order'daki müşteri bilgilerini kullan
                    order.UserName = order.CustomerName ?? "Üye Olmayan Kullanıcı";
                    order.UserEmail = order.CustomerEmail ?? "guest@customer.com";
                }

                // ShippingService üzerinden kargo firmasından bilgi almayı dene
                if (!string.IsNullOrEmpty(order.TrackingNumber) && !string.IsNullOrEmpty(order.ShippingCarrier))
                {
                    try
                    {
                        var trackingData = await shippingService.GetTrackingInfo(order.TrackingNumber, order.ShippingCarrier);
                        if (trackingData != null)
                        {
                            logger?.LogInformation($"Kargo takip API yanıtı alındı: {trackingData}");
                            // trackingData içindeki bilgileri response'a ekleyebilirsiniz
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, $"Kargo takip API'si çağrılırken hata oluştu: {order.TrackingNumber}");
                    }
                }

                response.Response = order;
                response.GenerateSuccess("Sipariş ve kargo takip bilgisi başarıyla getirildi.");
                return response;
            }
            catch (Exception ex)
            {
                 logger?.LogError(ex, "GetTrackingInfoOperation sırasında hata oluştu.");
                 response.GenerateError(5400, $"Kargo takip bilgisi alınırken hata: {ex.Message}");
                 return response;
            }
        }

        // Kullanıcı için Kendi Sipariş Detaylarını Alma (Kullanıcı Dostu)
        public static async Task<BaseResponse> GetMyOrderDetailsOperation(string token, int orderId, GeneralContext dbContext, UsersContext usersContext, ShippingService shippingService, ILogger logger)
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

                // Kullanıcı sadece kendi siparişini görebilir
                var order = await dbContext.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.Id == orderId && o.UserId == userIdInt)
                    .Select(o => new
                    {
                        OrderId = o.Id,
                        MerchantOid = o.MerchantOid,
                        OrderDate = o.OrderDate,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status.ToString(),
                        ShippingAddress = o.ShippingAddress,
                        BillingAddress = o.BillingAddress,
                        TrackingNumber = o.TrackingNumber,
                        ShippingCarrier = o.ShippingCarrier,
                        ShippedDate = o.ShippedDate,
                        DeliveredDate = o.DeliveredDate,
                        CancellationReason = o.CancellationReason,
                        ReturnReason = o.ReturnReason,
                        Items = o.OrderItems.Select(oi => new OrderItemResponse
                        {
                            ProductId = oi.ProductId,
                            ProductName = oi.ProductName,
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice
                        }).ToList(),
                        // Kargo durumu bilgileri
                        CanBeCancelled = o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing || o.Status == OrderStatus.Paid,
                        CanReturnRequest = o.Status == OrderStatus.Delivered && o.DeliveredDate.HasValue && 
                                         (DateTime.UtcNow - o.DeliveredDate.Value).TotalDays <= 14,
                        IsTrackable = !string.IsNullOrEmpty(o.TrackingNumber) && !string.IsNullOrEmpty(o.ShippingCarrier)
                    })
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    response.GenerateError(4201, "Sipariş bulunamadı veya yetkiniz yok.");
                    return response;
                }

                // Kargo takip bilgisini almayı dene
                string? trackingDetails = null;
                if (order.IsTrackable)
                {
                    try
                    {
                        var trackingData = await shippingService.GetTrackingInfo(order.TrackingNumber!, order.ShippingCarrier!);
                        if (trackingData != null)
                        {
                            trackingDetails = trackingData.ToString();
                            logger?.LogInformation($"Kargo takip API yanıtı alındı: {trackingData}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, $"Kargo takip API'si çağrılırken hata oluştu: {order.TrackingNumber}");
                    }
                }

                // Kullanıcı dostu response oluştur
                var userFriendlyResponse = new
                {
                    order.OrderId,
                    order.MerchantOid,
                    order.OrderDate,
                    order.TotalAmount,
                    order.Status,
                    order.ShippingAddress,
                    order.BillingAddress,
                    order.Items,
                    
                    // Kargo bilgileri
                    Shipping = new
                    {
                        order.TrackingNumber,
                        order.ShippingCarrier,
                        order.ShippedDate,
                        order.DeliveredDate,
                        TrackingDetails = trackingDetails,
                        order.IsTrackable
                    },
                    
                    // Kullanıcı işlemleri
                    Actions = new
                    {
                        order.CanBeCancelled,
                        order.CanReturnRequest
                    },
                    
                    // İptal/İade sebepleri (varsa)
                    order.CancellationReason,
                    order.ReturnReason
                };

                response.Response = userFriendlyResponse;
                response.GenerateSuccess("Sipariş detayları başarıyla getirildi.");
                return response;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "GetMyOrderDetailsOperation sırasında hata oluştu.");
                response.GenerateError(5400, $"Sipariş detayları alınırken hata: {ex.Message}");
                return response;
            }
        }

        // Admin için Teslim Edilmemiş Siparişleri Listeleme
        public static async Task<BaseResponse> GetNonDeliveredOrdersForAdminOperation(
            string token, 
            GetNonDeliveredOrdersRequest request, 
            GeneralContext dbContext, 
            UsersContext usersContext, 
            ILogger logger)
        {
            var response = new BaseResponse();
            try
            {
                // Admin yetkisi kontrolü
                if (!IsAdmin(token, usersContext))
                {
                    response.GenerateError(1001, "Yetkisiz işlem. Admin girişi gereklidir.");
                    return response;
                }

                // Sipariş durumuna göre filtreleme için başlangıç sorgusu oluştur
                var query = dbContext.Orders.Include(o => o.OrderItems).AsQueryable();

                // Eğer özel bir status filtresi yoksa, sadece teslim edilmemiş olanları getir
                if (!request.OrderStatus.HasValue)
                {
                    query = query.Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Failed);
                }

                // İsteğe bağlı filtreler
                if (request.OrderStatus.HasValue)
                {
                    query = query.Where(o => o.Status == request.OrderStatus.Value);
                }

                if (request.FromDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate >= request.FromDate.Value);
                }

                if (request.ToDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate <= request.ToDate.Value);
                }

                if (request.UserId.HasValue)
                {
                    query = query.Where(o => o.UserId == request.UserId.Value);
                }

                // Toplam sipariş sayısı
                var totalCount = await query.CountAsync();

                // Sıralama
                query = request.SortByDateAsc 
                    ? query.OrderBy(o => o.OrderDate) 
                    : query.OrderByDescending(o => o.OrderDate);

                // Sayfalama
                var pageNumber = Math.Max(1, request.PageNumber);
                var pageSize = Math.Max(10, Math.Min(50, request.PageSize));
                var skip = (pageNumber - 1) * pageSize;
                
                // Siparişleri getir
                var orders = await query
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(o => new OrderAdminResponse
                    {
                        OrderId = o.Id,
                        MerchantOid = o.MerchantOid,
                        UserId = o.UserId,
                        OrderDate = o.OrderDate,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status.ToString(),
                        ShippingAddress = o.ShippingAddress,
                        BillingAddress = o.BillingAddress,
                        TrackingNumber = o.TrackingNumber,
                        ShippingCarrier = o.ShippingCarrier,
                        ShippedDate = o.ShippedDate,
                        DeliveredDate = o.DeliveredDate,
                        CancellationReason = o.CancellationReason,
                        ReturnReason = o.ReturnReason,
                        CustomerName = o.CustomerName,
                        CustomerEmail = o.CustomerEmail,
                        Items = o.OrderItems.Select(oi => new OrderItemResponse
                        {
                            ProductId = oi.ProductId,
                            ProductName = oi.ProductName,
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice
                        }).ToList()
                    })
                    .ToListAsync();

                // Kullanıcı bilgilerini getirmek isterseniz, user ID'leri toplayıp bir sorgu daha yapabilirsiniz
                if (orders.Any() && usersContext != null)
                {
                    var userIds = orders.Where(o => o.UserId.HasValue).Select(o => o.UserId!.Value).Distinct().ToList();
                    
                    if (userIds.Any())
                    {
                        var users = await usersContext.AllUsersModel.Where(u => userIds.Contains(u.Id))
                            .Select(u => new { u.Id, u.UserName, u.Email })
                            .ToListAsync();

                        var userDict = users.ToDictionary(u => u.Id, u => new { u.UserName, u.Email });

                        // Kullanıcı bilgilerini sipariş yanıtına ekle
                        foreach (var order in orders)
                        {
                            if (order.UserId.HasValue && userDict.TryGetValue(order.UserId.Value, out var userInfo))
                            {
                                order.UserName = userInfo.UserName;
                                order.UserEmail = userInfo.Email;
                            }
                            else if (!order.UserId.HasValue)
                            {
                                // Üye olmayan kullanıcılar için Order'daki müşteri bilgilerini kullan
                                order.UserName = order.CustomerName ?? "Üye Olmayan Kullanıcı";
                                order.UserEmail = order.CustomerEmail ?? "guest@customer.com";
                            }
                        }
                    }
                }

                // Yanıt
                var result = new
                {
                    Data = orders,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasPreviousPage = pageNumber > 1,
                    HasNextPage = pageNumber < (int)Math.Ceiling((double)totalCount / pageSize)
                };

                response.Response = result;
                response.GenerateSuccess($"Siparişler listelendi: {orders.Count()} sipariş gösteriliyor. Toplam {totalCount} sipariş mevcut.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "GetNonDeliveredOrdersForAdminOperation sırasında hata oluştu.");
                response.GenerateError(5205, $"Siparişler getirilirken hata: {ex.Message}");
            }
            return response;
        }

        // Admin yetkisi kontrolü
        private static bool IsAdmin(string token, UsersContext usersContext)
        {
            var session = SessionService.TestToken(token, usersContext);
            return session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.AdminAdmin ||
                   session?._user?.UserRoleinAuthorization == UserRoleinAuthorization.Admin;
        }

        // Yeni: Sipariş Oluşturma (Ödeme Öncesi)
        public static async Task<BaseResponse> CreateOrderOperation(string? token, CreateOrderRequest request, GeneralContext dbContext, UsersContext usersContext, ILogger logger, EmailService emailService, string? callbackBaseUrl = null)
        {
            var response = new BaseResponse();
            try
            {
                int? userIdInt = null;
                AllUsersModels? user = null;
                ShoppingCart? cart = null;

                // Token varsa üye kullanıcı, yoksa üye olmayan kullanıcı
                if (!string.IsNullOrEmpty(token))
                {
                    var currentSession = SessionService.TestToken(token, usersContext);
                    if (currentSession == null)
                    {
                        response.GenerateError(1000, "Token geçersiz veya oturum süresi dolmuş.");
                        return response;
                    }
                    userIdInt = currentSession._user.Id;
                    user = currentSession._user;

                    // Üye kullanıcının sepetini al
                    cart = await dbContext.ShoppingCarts
                                          .Include(c => c.Items)
                                          .FirstOrDefaultAsync(c => c.UserId == userIdInt);
                }
                else
                {
                    // Üye olmayan kullanıcı - gerekli bilgileri kontrol et
                    if (string.IsNullOrEmpty(request.CustomerEmail) || string.IsNullOrEmpty(request.CustomerName))
                    {
                        response.GenerateError(4105, "Üye olmayan kullanıcılar için müşteri adı ve e-posta adresi gereklidir.");
                        return response;
                    }

                    // GuestCartId ile sepeti bul
                    if (!string.IsNullOrEmpty(request.GuestCartId))
                    {
                        cart = await dbContext.ShoppingCarts
                                              .Include(c => c.Items)
                                              .FirstOrDefaultAsync(c => c.GuestId == request.GuestCartId);
                    }
                    else
                    {
                        // GuestCartId yoksa sipariş oluşturulamaz (sepet bulunamaz)
                        response.GenerateError(4102, "Üye olmayan kullanıcılar için sepet bilgisi bulunamadı. Lütfen önce ürünleri sepete ekleyiniz.");
                        return response;
                    }
                }

                if (cart == null || !cart.Items.Any())
                {
                    response.GenerateError(4101, "Sepet boş veya bulunamadı.");
                    return response;
                }

                // Toplam tutarı sepet üzerinden hesapla
                decimal totalAmount = cart.Items.Sum(i => i.Quantity * i.UnitPrice);

                // Sipariş için müşteri bilgilerini belirle
                string customerName, customerEmail, customerPhone;
                
                if (user != null)
                {
                    // Üye kullanıcı - profil bilgilerini kullan
                    customerName = !string.IsNullOrWhiteSpace(user.UserName) ? user.UserName : "Ad Soyad";
                    customerEmail = user.Email ?? "no-reply@example.com";
                    customerPhone = user.PhoneNumber ?? "0000000000";
                }
                else
                {
                    // Üye olmayan kullanıcı - request'ten al
                    customerName = request.CustomerName!;
                    customerEmail = request.CustomerEmail!;
                    customerPhone = request.CustomerPhone ?? "0000000000";
                }

                // Siparişi oluştur
                string merchantOid = Guid.NewGuid().ToString();
                var order = new Order
                {
                    MerchantOid = merchantOid,
                    UserId = userIdInt, // null olabilir (üye olmayan kullanıcılar için)
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = totalAmount,
                    Status = OrderStatus.Pending, // Ödeme bekleniyor
                    ShippingAddress = request.ShippingAddress,
                    BillingAddress = request.BillingAddress ?? request.ShippingAddress,
                    CustomerName = customerName,
                    CustomerEmail = customerEmail,
                    CustomerPhone = customerPhone,
                    OrderItems = cart.Items.Select(i => new OrderItem
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        UnitPrice = i.UnitPrice,
                        Quantity = i.Quantity
                    }).ToList()
                };

                dbContext.Orders.Add(order);
                await dbContext.SaveChangesAsync();
                
                logger?.LogInformation($"Yeni sipariş oluşturuldu: {merchantOid}, Kullanıcı: {userIdInt?.ToString() ?? "Üye Olmayan"}, Email: {customerEmail}");

                // Sipariş başarıyla oluşturuldu, sepeti temizle
                if(cart != null)
                {
                    dbContext.CartItems.RemoveRange(cart.Items);
                    dbContext.ShoppingCarts.Remove(cart);
                    logger?.LogInformation($"Kullanıcının sepeti temizlendi: UserID={userIdInt}, OrderID={order.Id}");
                }
                
                await dbContext.SaveChangesAsync();

                var createOrderResponse = new CreateOrderResponse
                {
                    OrderId = order.Id,
                    MerchantOid = order.MerchantOid,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status.ToString(),
                    OrderDate = order.OrderDate
                };

                response.Response = createOrderResponse;
                response.GenerateSuccess("Sipariş başarıyla oluşturuldu.");

                // E-posta Bildirimleri
                try
                {
                    // Yöneticiye bildirim (CustomerName null ise varsayılan değer kullanılır)
                    string adminNotificationCustomerName = !string.IsNullOrEmpty(order.CustomerName) ? order.CustomerName : "Bir Müşteri";
                    await emailService.SendOrderNotificationAsync(order.Id, order.MerchantOid, adminNotificationCustomerName, order.TotalAmount);
                    logger?.LogInformation($"Yöneticiye yeni sipariş bildirimi gönderildi: OrderID={order.Id}");

                    // Kullanıcıya sipariş onayı (CustomerEmail null/boş ise gönderilmez)
                    if (!string.IsNullOrEmpty(order.CustomerEmail))
                    {
                        var productNames = order.OrderItems.Select(oi => oi.ProductName).ToList();
                        string userConfirmationCustomerName = !string.IsNullOrEmpty(order.CustomerName) ? order.CustomerName : "Değerli Müşterimiz";
                        await emailService.SendOrderConfirmationToUserAsync(order.CustomerEmail, userConfirmationCustomerName, order.Id, order.TotalAmount, productNames, order.ShippingAddress ?? "Adres belirtilmemiş");
                        logger?.LogInformation($"Kullanıcıya sipariş onayı gönderildi: OrderID={order.Id}, Email: {order.CustomerEmail}");
                    }
                    else
                    {
                        logger?.LogWarning($"Kullanıcı e-postası boş olduğu için sipariş onayı gönderilemedi: OrderID={order.Id}");
                    }
                }
                catch (Exception emailEx)
                {
                    logger?.LogError(emailEx, $"Sipariş oluşturma sonrası e-posta gönderiminde hata: OrderID={order.Id}");
                    // E-posta hatası ana işlemi etkilememeli, sadece loglanmalı
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "CreateOrderOperation sırasında hata oluştu.");
                response.GenerateError(5300, $"Sipariş oluşturulurken hata: {ex.Message}");
            }
            return response;
        }

        // Yeni Operasyon: Kargo Takip Numarası Yollama (Admin)
        public static async Task<BaseResponse> SendTrackingNumberToUserOperation(string token, SendTrackingNumberRequest request, GeneralContext dbContext, UsersContext usersContext, ILogger logger, EmailService emailService, string? callbackBaseUrl = null)
        {
            var response = new BaseResponse();
            try
            {
                // Admin yetkisi kontrolü
                if (!IsAdmin(token, usersContext))
                {
                    response.GenerateError(1001, "Yetkisiz işlem. Admin girişi gereklidir.");
                    return response;
                }

                var order = await dbContext.Orders.FindAsync(request.OrderId);
                if (order == null)
                {
                    response.GenerateError(4201, "Sipariş bulunamadı.");
                    return response;
                }

                // Kargo bilgilerini güncelle (UpdateShippingInfoOperation'a benzer, ama sadece kargo no ve firma odaklı)
                var oldStatus = order.Status;
                order.TrackingNumber = request.TrackingNumber;
                order.ShippingCarrier = request.ShippingCarrier;
                // Sipariş durumunu Shipped yapalım, eğer zaten daha ileride bir durumda değilse.
                if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Cancelled && order.Status != OrderStatus.Failed && order.Status != OrderStatus.Refunded && order.Status != OrderStatus.PartiallyRefunded)
                {
                    order.Status = OrderStatus.Shipped;
                    if (!order.ShippedDate.HasValue) // Eğer daha önce kargoya verilmediyse, tarih ata
                    {
                        order.ShippedDate = DateTime.UtcNow;
                    }
                }
                
                await dbContext.SaveChangesAsync();
                logger?.LogInformation($"Admin tarafından kargo takip numarası eklendi/güncellendi: OrderID={order.Id}, KargoNo={request.TrackingNumber}, Firma={request.ShippingCarrier}");

                // Kullanıcıya e-posta gönder
                try
                {
                    if (!string.IsNullOrEmpty(order.CustomerEmail))
                    {
                        string? orderDetailsUrl = !string.IsNullOrEmpty(callbackBaseUrl) ? $"{callbackBaseUrl}/orders/{order.Id}" : null;
                        string customerNameForEmail = !string.IsNullOrEmpty(order.CustomerName) ? order.CustomerName : "Değerli Müşterimiz";
                        await emailService.SendOrderStatusUpdateToUserAsync(
                            order.CustomerEmail, 
                            customerNameForEmail, 
                            order.Id, 
                            "Shipped", // Durumu "Kargoya Verildi" olarak gönderiyoruz
                            order.TrackingNumber, 
                            order.ShippingCarrier, 
                            orderDetailsUrl);
                        logger?.LogInformation($"Kullanıcıya kargo takip numarası ({request.TrackingNumber}) e-postası gönderildi: OrderID={order.Id}");
                    }
                    else
                    {
                        logger?.LogWarning($"Kullanıcı e-postası boş olduğu için kargo takip numarası e-postası gönderilemedi: OrderID={order.Id}");
                    }
                }
                catch (Exception emailEx)
                {
                    logger?.LogError(emailEx, $"Kargo takip numarası yollama sonrası e-posta gönderiminde hata: OrderID={order.Id}");
                    // E-posta hatası olsa bile operasyon başarılı sayılabilir, loglamak yeterli.
                }

                response.GenerateSuccess($"Kargo takip numarası başarıyla güncellendi ve kullanıcıya e-posta gönderildi: {order.CustomerEmail}.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "SendTrackingNumberToUserOperation sırasında hata oluştu.");
                response.GenerateError(5206, $"Kargo takip numarası yollanırken hata: {ex.Message}");
            }
            return response;
        }
    }
} 