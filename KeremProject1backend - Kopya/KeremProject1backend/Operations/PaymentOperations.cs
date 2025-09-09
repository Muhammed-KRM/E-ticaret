using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using KeremProject1backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Loglama için
using System; // Guid için
using System.Linq;
using System.Net.Http; // HttpClient için
using System.Text.Json; // JsonSerializer için
using System.Threading.Tasks;
using System.Collections.Generic; // Dictionary için

namespace KeremProject1backend.Operations
{
    public static class PaymentOperations
    {
        private static readonly HttpClient _httpClient = new HttpClient(); // PayTR API isteği için

        public static async Task<BaseResponse> InitiatePaymentOperation(string? token, InitiatePaymentRequest request, GeneralContext dbContext, UsersContext usersContext, PayTrService payTrService, ILogger logger, string userIp, string callbackBaseUrl)
        {
            var response = new BaseResponse();
            try
            {
                // Siparişi bul
                var order = await dbContext.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId);

                if (order == null)
                {
                    response.GenerateError(4001, "Sipariş bulunamadı.");
                    return response;
                }

                // Sipariş durumu kontrolü
                if (order.Status != OrderStatus.Pending)
                {
                    response.GenerateError(4002, $"Bu sipariş için ödeme yapılamaz. Durum: {order.Status}");
                    return response;
                }

                // Token varsa kullanıcı kontrolü (opsiyonel)
                if (!string.IsNullOrEmpty(token))
                {
                    var currentSession = SessionService.TestToken(token, usersContext);
                    if (currentSession != null && order.UserId != currentSession._user.Id)
                    {
                        response.GenerateError(4003, "Bu sipariş size ait değil.");
                        return response;
                    }
                }

                // PayTR için sepet ve ödeme detaylarını hazırla
                string paymentAmountStr = (order.TotalAmount * 100).ToString("0");
                
                // Sipariş öğelerini PayTR formatına çevir
                var cartItems = order.OrderItems.Select(oi => new CartItem
                {
                    ProductName = oi.ProductName,
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity
                }).ToList();
                
                string userBasketStr = payTrService.FormatUserBasket(cartItems);
                string noInstallment = "0";
                string maxInstallment = "0";
                string currency = "TL";
                string testMode = "1"; // Test modu
                
                string successUrl = $"{callbackBaseUrl}/payment/success?oid={order.MerchantOid}";
                string failUrl = $"{callbackBaseUrl}/payment/fail?oid={order.MerchantOid}";
                string callbackUrl = $"{callbackBaseUrl}/api/payment/callback";

                // PayTR Token Hash oluştur
                string payTrTokenHash = payTrService.GeneratePayTrHash(
                    order.MerchantOid, 
                    order.CustomerEmail ?? "no-reply@example.com", 
                    paymentAmountStr, 
                    userBasketStr, 
                    noInstallment, 
                    maxInstallment, 
                    userIp, 
                    currency, 
                    testMode, 
                    callbackUrl);

                // PayTR API isteği için verileri hazırla
                var paytrRequestData = new Dictionary<string, string>
                {
                    { "merchant_id", payTrService.GetMerchantId() },
                    { "user_ip", userIp },
                    { "merchant_oid", order.MerchantOid },
                    { "email", order.CustomerEmail ?? "no-reply@example.com" },
                    { "payment_amount", paymentAmountStr },
                    { "paytr_token", payTrTokenHash },
                    { "user_basket", userBasketStr },
                    { "debug_on", testMode },
                    { "client_lang", "tr" },
                    { "no_installment", noInstallment },
                    { "max_installment", maxInstallment },
                    { "user_name", order.CustomerName ?? "Müşteri" },
                    { "user_address", order.ShippingAddress ?? "Adres Yok" },
                    { "user_phone", order.CustomerPhone ?? "0000000000" },
                    { "merchant_ok_url", successUrl },
                    { "merchant_fail_url", failUrl },
                    { "timeout_limit", "30" },
                    { "currency", currency },
                    { "test_mode", testMode }
                };

                // PayTR API'ye isteği gönder
                var content = new FormUrlEncodedContent(paytrRequestData);
                logger?.LogInformation($"PayTR get-token isteği gönderiliyor: {order.MerchantOid}");
                var paytrApiResponse = await _httpClient.PostAsync("https://www.paytr.com/odeme/api/get-token", content);
                var responseString = await paytrApiResponse.Content.ReadAsStringAsync();

                // PayTR API yanıtını işle
                if (!paytrApiResponse.IsSuccessStatusCode)
                {
                    logger?.LogError($"PayTR get-token API hatası ({paytrApiResponse.StatusCode}): {responseString}");
                    response.GenerateError(4102, $"Ödeme başlatılamadı (API Hatası): {responseString}");
                    return response;
                }

                var paytrResponse = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseString);

                if (paytrResponse == null || !paytrResponse.TryGetValue("status", out var statusElement) || statusElement.GetString() != "success")
                {
                    string reason = paytrResponse?.ContainsKey("reason") == true ? (paytrResponse["reason"].GetString() ?? "Detay Yok") : "Bilinmeyen hata";
                    logger?.LogError($"PayTR get-token başarısız: {reason} (Yanıt: {responseString})");
                    response.GenerateError(4103, $"Ödeme başlatılamadı: {reason}");
                    return response;
                }

                // Başarılı yanıtı oluştur
                if (!paytrResponse.TryGetValue("token", out var tokenElement) || tokenElement.ValueKind != JsonValueKind.String)
                {
                    logger?.LogError($"PayTR get-token yanıtında geçerli 'token' alanı bulunamadı. Yanıt: {responseString}");
                    response.GenerateError(4104, "Ödeme token'ı alınamadı.");
                    return response;
                }
                string iframeToken = tokenElement.GetString()!;
                logger?.LogInformation($"PayTR iframe token alındı: {order.MerchantOid}");

                var successResponse = new InitiatePaymentResponse
                {
                    PayTrToken = iframeToken,
                    MerchantOid = order.MerchantOid
                };
                response.Response = successResponse;
                response.GenerateSuccess("Ödeme başarıyla başlatıldı.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "InitiatePaymentOperation sırasında beklenmeyen hata oluştu.");
                response.GenerateError(5100, $"Ödeme başlatılırken hata: {ex.Message}");
            }
            return response;
        }

        public static async Task<bool> HandleCallbackOperation(PayTrCallbackRequest request, GeneralContext dbContext, PayTrService payTrService, ILogger logger)
        {
            logger?.LogInformation($"PayTR callback alındı: OID={request.merchant_oid}, Status={request.status}");

            bool isHashValid = payTrService.ValidateCallbackHash(request.hash, request.merchant_oid, request.status, request.total_amount);

            if (!isHashValid)
            {
                logger?.LogWarning($"PayTR callback hash doğrulaması BAŞARISIZ! OID: {request.merchant_oid}");
                return false;
            }
            logger?.LogInformation($"PayTR callback hash doğrulaması başarılı. OID: {request.merchant_oid}");

            var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.MerchantOid == request.merchant_oid);
            if (order == null)
            {
                logger?.LogError($"PayTR callback için sipariş bulunamadı! OID: {request.merchant_oid}");
                return false;
            }

            // Check if order is already processed
            if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Processing || order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Refunded || order.Status == OrderStatus.PartiallyRefunded)
            {
                logger?.LogWarning($"PayTR callback: Sipariş zaten işlenmiş veya iade edilmiş durumda. OID: {request.merchant_oid}, Mevcut Durum: {order.Status}");
                return true; // Already handled
            }

            if (request.status == "success")
            {
                order.Status = OrderStatus.Paid;
                // PayTR sends amount in kurus, convert to decimal TL
                decimal paidAmount = decimal.Parse(request.payment_amount ?? request.total_amount) / 100m;
                if (paidAmount != order.TotalAmount)
                {
                     logger?.LogWarning($"PayTR callback: Ödeme tutarı eşleşmiyor! OID: {request.merchant_oid}, Beklenen: {order.TotalAmount}, Gelen: {paidAmount}");
                     // Potentially handle discrepancy, but proceed with marking as paid for now
                }

                logger?.LogInformation($"PayTR callback: Sipariş durumu 'Paid' olarak güncellendi. OID: {request.merchant_oid}");

                // Clear the user's cart after successful payment
                ShoppingCart? cart = null;
                
                if (order.UserId.HasValue)
                {
                    // Üye kullanıcının sepetini temizle
                    cart = await dbContext.ShoppingCarts
                        .Include(c => c.Items)
                        .FirstOrDefaultAsync(c => c.UserId == order.UserId.Value);
                }
                else
                {
                    // Üye olmayan kullanıcının sepetini temizle
                    // Order'dan GuestCartId bilgisini alamayız çünkü sipariş oluştuktan sonra silinir
                    // Bu durumda, geçici sepetleri temizlemek için ayrı bir job/service kullanılabilir
                    // Şimdilik bu kısmı atla, çünkü sipariş oluştuktan sonra guest sepet zaten kullanılmaz
                    logger?.LogInformation($"Üye olmayan kullanıcının siparişi tamamlandı: OID={order.MerchantOid}");
                }
                    
                if(cart != null)
                {
                    dbContext.CartItems.RemoveRange(cart.Items);
                    dbContext.ShoppingCarts.Remove(cart);
                     logger?.LogInformation($"Kullanıcının sepeti temizlendi: UserID={order.UserId}, OID={request.merchant_oid}");
                }
            }
            else // Payment failed
            {
                order.Status = OrderStatus.Failed;
                logger?.LogWarning($"PayTR callback: Ödeme başarısız oldu. OID: {request.merchant_oid}, Sebep Kodu: {request.failed_reason_code}, Mesaj: {request.failed_reason_msg}");
            }

            await dbContext.SaveChangesAsync();
            return true;
        }

        // E-posta bildirimi ile birlikte ödeme callback'i işleme
        public static async Task<bool> HandlePayTrCallbackWithEmailAsync(
            PayTrCallbackRequest request, 
            GeneralContext dbContext, 
            PayTrService payTrService, 
            ILogger logger,
            EmailService emailService)
        {
            logger?.LogInformation($"PayTR callback alındı: OID={request.merchant_oid}, Status={request.status}");

            bool isHashValid = payTrService.ValidateCallbackHash(request.hash, request.merchant_oid, request.status, request.total_amount);

            if (!isHashValid)
            {
                logger?.LogWarning($"PayTR callback hash doğrulaması BAŞARISIZ! OID: {request.merchant_oid}");
                return false;
            }
            logger?.LogInformation($"PayTR callback hash doğrulaması başarılı. OID: {request.merchant_oid}");

            var order = await dbContext.Orders
                .Include(o => o.OrderItems) // Sipariş öğelerini dahil et
                .FirstOrDefaultAsync(o => o.MerchantOid == request.merchant_oid);
                
            if (order == null)
            {
                logger?.LogError($"PayTR callback için sipariş bulunamadı! OID: {request.merchant_oid}");
                return false;
            }

            // Sipariş zaten işlendiyse yeniden işleme
            if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Processing || 
                order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered || 
                order.Status == OrderStatus.Refunded || order.Status == OrderStatus.PartiallyRefunded)
            {
                logger?.LogWarning($"PayTR callback: Sipariş zaten işlenmiş veya iade edilmiş durumda. OID: {request.merchant_oid}, Mevcut Durum: {order.Status}");
                return true; // Zaten işlenmiş
            }

            if (request.status == "success")
            {
                order.Status = OrderStatus.Paid;
                // PayTR kuruş olarak gönderir, TL'ye çevir
                decimal paidAmount = decimal.Parse(request.payment_amount ?? request.total_amount) / 100m;
                if (paidAmount != order.TotalAmount)
                {
                     logger?.LogWarning($"PayTR callback: Ödeme tutarı eşleşmiyor! OID: {request.merchant_oid}, Beklenen: {order.TotalAmount}, Gelen: {paidAmount}");
                }

                logger?.LogInformation($"PayTR callback: Sipariş durumu 'Paid' olarak güncellendi. OID: {request.merchant_oid}");

                // Kullanıcının sepetini temizle
                ShoppingCart? cart = null;
                
                if (order.UserId.HasValue)
                {
                    // Üye kullanıcının sepetini temizle
                    cart = await dbContext.ShoppingCarts
                    .Include(c => c.Items)
                        .FirstOrDefaultAsync(c => c.UserId == order.UserId.Value);
                }
                else
                {
                    // Üye olmayan kullanıcının sepetini temizle
                    // Order'dan GuestCartId bilgisini alamayız çünkü sipariş oluştuktan sonra silinir
                    // Bu durumda, geçici sepetleri temizlemek için ayrı bir job/service kullanılabilir
                    // Şimdilik bu kısmı atla, çünkü sipariş oluştuktan sonra guest sepet zaten kullanılmaz
                    logger?.LogInformation($"Üye olmayan kullanıcının siparişi tamamlandı: OID={order.MerchantOid}");
                }
                    
                if(cart != null)
                {
                    dbContext.CartItems.RemoveRange(cart.Items);
                    dbContext.ShoppingCarts.Remove(cart);
                    logger?.LogInformation($"Kullanıcının sepeti temizlendi: UserID={order.UserId}, OID={request.merchant_oid}");
                }

                // Değişiklikleri kaydet
                await dbContext.SaveChangesAsync();
                
                // E-posta bildirimini gönder
                try
                {
                    // Müşteri adı için basit bir format kullan
                    string customerName = !string.IsNullOrEmpty(order.CustomerName) ? order.CustomerName : $"Müşteri (ID: {order.UserId})";
                    
                    // Yöneticiye bildirim (bu zaten vardı)
                    var adminEmailSent = await emailService.SendOrderNotificationAsync(
                        order.Id,
                        order.MerchantOid,
                        customerName, // Yöneticiye gidecek e-postada bu isim kullanılabilir
                        order.TotalAmount);
                        
                    if (adminEmailSent)
                    {
                        logger?.LogInformation($"Yöneticiye yeni sipariş bildirimi e-posta ile gönderildi: OrderID={order.Id}");
                    }
                    else
                    {
                        logger?.LogWarning($"Yönetici sipariş bildirimi e-postası gönderilemedi: OrderID={order.Id}");
                    }

                    // Kullanıcıya sipariş onayı e-postası
                    if (!string.IsNullOrEmpty(order.CustomerEmail))
                    {
                        var productNames = order.OrderItems.Select(oi => oi.ProductName).ToList();
                        var userEmailSent = await emailService.SendOrderConfirmationToUserAsync(
                            order.CustomerEmail, 
                            customerName, // Kullanıcıya gidecek e-postada da bu isim kullanılabilir
                            order.Id, 
                            order.TotalAmount, 
                            productNames, 
                            order.ShippingAddress ?? "Adres belirtilmemiş");

                        if (userEmailSent)
                        {
                            logger?.LogInformation($"Kullanıcıya sipariş onayı e-postası gönderildi: OrderID={order.Id}, Email: {order.CustomerEmail}");
                        }
                        else
                        {
                            logger?.LogWarning($"Kullanıcıya sipariş onayı e-postası gönderilemedi: OrderID={order.Id}, Email: {order.CustomerEmail}");
                        }
                    }
                    else
                    {
                        logger?.LogWarning($"Kullanıcı e-posta adresi bulunamadığı için sipariş onayı gönderilemedi: OrderID={order.Id}");
                    }
                }
                catch (Exception ex)
                {
                    // E-posta gönderilemese bile işleme devam et, sadece log tut
                    logger?.LogError(ex, $"Sipariş e-posta bildirimi gönderilirken hata oluştu: OrderID={order.Id}");
                }
            }
            else // Ödeme başarısız
            {
                order.Status = OrderStatus.Failed;
                logger?.LogWarning($"PayTR callback: Ödeme başarısız oldu. OID: {request.merchant_oid}, Sebep Kodu: {request.failed_reason_code}, Mesaj: {request.failed_reason_msg}");
                await dbContext.SaveChangesAsync();
            }

            return true;
        }
    }
} 