using System.Security.Cryptography;
using System.Text;
using KeremProject1backend.Models.DBs; // ConfigDef ve CartItem için
using Microsoft.Extensions.Options; // IOptions için
using System.Text.Json; // JSON serileştirme için
using System.Collections.Generic;
using System.Linq;
using System.Net.Http; // HttpClient için eklendi
using System.Threading.Tasks; // Task için eklendi
using Microsoft.Extensions.Logging; // ILogger için eklendi
using KeremProject1backend.Models.Requests;

namespace KeremProject1backend.Services
{
    public class PayTrService
    {
        private readonly ConfigDef _config;
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly ILogger<PayTrService> _logger; // Logger eklendi

        public PayTrService(IOptions<ConfigDef> configOptions, ILogger<PayTrService> logger) // HttpClient ve Logger eklendi
        {
            _config = configOptions.Value;
            _logger = logger;
        }

        // Operations tarafından MerchantId'ye erişim için public metot
        public string GetMerchantId() => _config.PayTrMerchantId;

        /// <summary>
        /// PayTR API istekleri için gerekli olan hash'i oluşturur.
        /// </summary>
        public string GeneratePayTrHash(string merchantOid, string email, string paymentAmount, string userBasket, string noInstallment, string maxInstallment, string userIp, string currency, string testMode, string callbackUrl)
        {
            string hashString = _config.PayTrMerchantId + // ConfigDef kullanıldı
                              userIp +
                              merchantOid +
                              email +
                              paymentAmount +
                              userBasket +
                              noInstallment +
                              maxInstallment +
                              currency +
                              testMode +
                              callbackUrl +
                              _config.PayTrMerchantSalt; // ConfigDef kullanıldı

            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_config.PayTrMerchantKey))) // ConfigDef kullanıldı
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(hashString));
                return Convert.ToBase64String(hashBytes);
            }
        }

        /// <summary>
        /// PayTR callback isteğindeki hash'i doğrular.
        /// </summary>
        public bool ValidateCallbackHash(string receivedHash, string merchantOid, string status, string totalAmount)
        {
            string hashString = merchantOid +
                              _config.PayTrMerchantSalt + // ConfigDef kullanıldı
                              status +
                              totalAmount;

            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_config.PayTrMerchantKey))) // ConfigDef kullanıldı
            {
                byte[] computedHashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(hashString));
                string computedHash = Convert.ToBase64String(computedHashBytes);
                return receivedHash == computedHash;
            }
        }

        /// <summary>
        /// Sepet içeriğini PayTR'ın user_basket formatına dönüştürür.
        /// Örnek Format: "[[\"Ürün Adı 1\",\"10.50\",1],[\"Ürün Adı 2\",\"20.00\",2]]"
        /// </summary>
        public string FormatUserBasket(IEnumerable<CartItem> cartItems)
        {
            var basketArray = cartItems.Select(item => new object[]
            {
                item.ProductName ?? "Ürün", // Ürün adı boş olmamalı
                (item.UnitPrice * 100).ToString("0"), // Fiyatı kuruşa çevir ve string yap
                item.Quantity
            }).ToArray();

            return JsonSerializer.Serialize(basketArray);
        }

        /// <summary>
        /// PayTR İade API'sine istek gönderir.
        /// </summary>
        /// <param name="merchantOid">İade edilecek siparişin numarası.</param>
        /// <param name="refundAmount">İade edilecek tutar (TL cinsinden).</param>
        /// <returns>İade işleminin başarı durumu.</returns>
        public async Task<(bool Success, string Message)> SendRefundRequestAsync(string merchantOid, decimal refundAmount)
        {
            try
            {
                string refundAmountStr = (refundAmount * 100).ToString("0"); // Kuruş cinsinden

                // İade için hash oluşturma
                string hashStr = _config.PayTrMerchantId + merchantOid + refundAmountStr + _config.PayTrMerchantSalt;
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_config.PayTrMerchantKey));
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(hashStr));
                string paytrToken = Convert.ToBase64String(hashBytes);

                // İade API isteği için veriler
                var refundRequestData = new Dictionary<string, string>
                {
                    { "merchant_id", _config.PayTrMerchantId },
                    { "merchant_oid", merchantOid },
                    { "return_amount", refundAmountStr },
                    { "paytr_token", paytrToken }
                };

                // PayTR İade API'sine POST isteği
                var content = new FormUrlEncodedContent(refundRequestData);
                 _logger?.LogInformation($"PayTR iade isteği gönderiliyor: OID={merchantOid}, Tutar={refundAmount}");

                var response = await _httpClient.PostAsync("https://www.paytr.com/odeme/iade", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogError($"PayTR iade API hatası ({response.StatusCode}): OID={merchantOid}, Yanıt={responseString}");
                    return (false, $"PayTR API hatası: {responseString}");
                }

                 // PayTR iade yanıtını parse et
                var paytrResponse = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseString);

                if (paytrResponse == null || !paytrResponse.ContainsKey("status") || paytrResponse["status"].GetString() != "success")
                {
                    string reason = paytrResponse?.ContainsKey("err_msg") == true ? paytrResponse["err_msg"].GetString() : "Bilinmeyen hata";
                    _logger?.LogError($"PayTR iade başarısız: OID={merchantOid}, Sebep={reason}, Yanıt={responseString}");
                    return (false, $"PayTR iade başarısız: {reason}");
                }

                 _logger?.LogInformation($"PayTR iade başarılı: OID={merchantOid}");
                return (true, "İade talebi başarıyla gönderildi.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"PayTR iade isteği gönderilirken hata oluştu: OID={merchantOid}");
                 return (false, $"İade sırasında beklenmeyen bir hata oluştu: {ex.Message}");
            }
        }
    }
} 