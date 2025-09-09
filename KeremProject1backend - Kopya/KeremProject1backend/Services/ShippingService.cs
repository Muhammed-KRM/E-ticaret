using KeremProject1backend.Models.DBs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Headers; // AuthenticationHeaderValue için
using System.Net.Http.Json; // ReadFromJsonAsync için
using System.Text.Json;
using System.Threading.Tasks;

namespace KeremProject1backend.Services
{
    public class ShippingService
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigDef _config;
        private readonly ILogger<ShippingService> _logger;

        public ShippingService(HttpClient httpClient, IOptions<ConfigDef> configOptions, ILogger<ShippingService> logger)
        {
            _httpClient = httpClient;
            _config = configOptions.Value;
            _logger = logger;
        }

        /// <summary>
        /// Kargo takip bilgisini alır (İskelet).
        /// </summary>
        /// <returns>API'den dönen takip verisi (JSON object veya belirli bir model) ya da null.</returns>
        public async Task<object?> GetTrackingInfo(string trackingNumber, string carrier) // carrier parametresi belki gerekmeyebilir, API'ye bağlı
        {
            if (string.IsNullOrEmpty(_config.ShippingApiUrl) || string.IsNullOrEmpty(trackingNumber))
            {
                _logger?.LogWarning("Kargo API URL'si veya takip numarası eksik.");
                return null;
            }

            // <!!! --- BURAYI KARGO FİRMANIN API DOKÜMANTASYONUNA GÖRE DOLDUR --- !!!>

            // 1. API Endpoint Adresini Oluştur
            // Örnek: string apiUrl = $"{_config.ShippingApiUrl}/track/{trackingNumber}";
            // Örnek: string apiUrl = $"{_config.ShippingApiUrl}/tracking?code={trackingNumber}";
            string apiUrl = $"{_config.ShippingApiUrl.TrimEnd('/')}/YOL/PARAMETRE?takipNo={trackingNumber}"; // <-- API ADRESİNİ DÜZENLE

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

                // 2. Kimlik Doğrulama (Gerekliyse)
                if (!string.IsNullOrEmpty(_config.ShippingApiKey))
                {
                    // Örnek: Bearer Token
                    // request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ShippingApiKey);

                    // Örnek: Header'a API Key ekleme
                    request.Headers.Add("X-API-Key", _config.ShippingApiKey); // <-- HEADER ADINI DÜZENLE

                    // Örnek: Basic Auth
                    // var basicAuth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_config.ShippingApiUser}:{_config.ShippingApiPassword}"));
                    // request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
                }

                _logger?.LogInformation($"Kargo API isteği gönderiliyor: {apiUrl}");
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    // 3. Yanıtı İşle (JSON veya başka bir formatta olabilir)
                    try
                    {
                        // Örnek: Yanıt JSON ise ve direkt bir nesneye map edilecekse:
                        // var trackingData = await response.Content.ReadFromJsonAsync<KargoApiYanitModeli>(); // <-- Kendi modelini oluştur veya object kullan

                        // Örnek: Yanıt JSON ise ama direkt string olarak işlenecekse:
                        var responseBody = await response.Content.ReadAsStringAsync();
                        _logger?.LogInformation($"Kargo API yanıtı ({response.StatusCode}): {responseBody}");
                        // Bu string'i parse edip istediğin bilgileri alabilirsin.
                        // object trackingData = JsonSerializer.Deserialize<object>(responseBody);
                        object trackingData = responseBody; // Şimdilik ham yanıtı dönelim

                        return trackingData;
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger?.LogError(jsonEx, "Kargo API yanıtı JSON olarak işlenemedi.");
                        return null;
                    }
                    catch (NotSupportedException formatEx) // ReadFromJsonAsync için
                    {
                         _logger?.LogError(formatEx, "Kargo API yanıt formatı desteklenmiyor.");
                         return null;
                    }
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger?.LogError($"Kargo API hatası ({response.StatusCode}): {errorBody}");
                    return null;
                }
            }
            catch (HttpRequestException httpEx)
            {
                 _logger?.LogError(httpEx, $"Kargo API isteği gönderilirken hata oluştu: {apiUrl}");
                 return null;
            }
            catch (Exception ex)
            {
                 _logger?.LogError(ex, "GetTrackingInfo sırasında beklenmeyen hata oluştu.");
                 return null;
            }
             // <!!! --- DOLDURULACAK ALAN SONU --- !!!>
        }
    }

    // İsteğe bağlı: Kargo API'sinden dönen JSON yanıtına uygun bir model oluşturabilirsin.
    // public class KargoApiYanitModeli
    // {
    //     public string Status { get; set; }
    //     public string Location { get; set; }
    //     public List<KargoHareket> History { get; set; }
    // }
    // public class KargoHareket
    // {
    //    public DateTime Date { get; set; }
    //    public string Description { get; set; }
    // }
} 