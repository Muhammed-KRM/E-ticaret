using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using KeremProject1backend.Operations;
using KeremProject1backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; // AllowAnonymous için eklendi

namespace KeremProject1backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly GeneralContext _dbContext;
        private readonly UsersContext _usersContext; // Eklendi
        private readonly PayTrService _payTrService;
        private readonly ILogger<PaymentController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService; // Yeni eklendi

        public PaymentController(
            GeneralContext dbContext,
            UsersContext usersContext, // Eklendi
            PayTrService payTrService,
            ILogger<PaymentController> logger,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            EmailService emailService) // Yeni eklendi
        {
            _dbContext = dbContext;
            _usersContext = usersContext; // Eklendi
            _payTrService = payTrService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _emailService = emailService; // Yeni eklendi
        }

        private string GetUserIpAddress()
        {
             // X-Forwarded-For header'ını kontrol et (proxy arkasındaysa)
            var forwardedHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"];
            if (!string.IsNullOrEmpty(forwardedHeader)){
                // İlk IP adresini al
                return (forwardedHeader?.ToString() ?? "").Split(',').FirstOrDefault()?.Trim() ?? "0.0.0.0";
            }
            // Yoksa direkt bağlantı IP'sini al
            return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        }

        private string GetCallbackBaseUrl()
        {
            // appsettings.json'dan veya direkt olarak alınabilir.
            // Örnek: return _configuration["AppSettings:CallbackBaseUrl"]; 
            // Veya Request şemasından ve hostundan oluşturulabilir:
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null) return ""; // HttpContext yoksa boş dön
            return $"{request.Scheme}://{request.Host}"; // Örn: https://localhost:7049
        }

        [HttpPost("initiate")]
        [Produces("application/json", Type = typeof(InitiatePaymentResponse))]
        public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentRequest request, [FromHeader] string? token)
        {
            string userIp = GetUserIpAddress();
            string callbackBaseUrl = GetCallbackBaseUrl();
            var result = await PaymentOperations.InitiatePaymentOperation(token, request, _dbContext, _usersContext, _payTrService, _logger, userIp, callbackBaseUrl);
            return Ok(result);
        }

        // PayTR Callback Endpoint'i
        // DİKKAT: Bu endpoint [AllowAnonymous] olmalı ve PayTR IP adreslerinden gelen isteklere izin vermelidir (güvenlik için).
        [HttpPost("callback")]
        [AllowAnonymous] // PayTR bu endpoint'e token olmadan POST yapacak
        public async Task<IActionResult> HandlePayTrCallback([FromForm] PayTrCallbackRequest request) // PayTrCallbackRequest modeli Models/Requests altında olmalı
        {
            // Not: [FromForm] attribute'u form verilerini modelle eşleştirmek için önemlidir.
            _logger.LogInformation($"Callback endpoint çağrıldı: OID={request.merchant_oid}");

            // Ödeme işlemi başarılı olduğunda e-posta gönderimi için PaymentOperations sınıfını güncelleyeceğiz
            var success = await PaymentOperations.HandlePayTrCallbackWithEmailAsync(
                request, 
                _dbContext, 
                _payTrService, 
                _logger, 
                _emailService);

            // PayTR'a her zaman "OK" yanıtı dönülmelidir.
            // Kendi içimizde hash doğrulama başarısız olsa bile.
            return Content("OK", "text/plain");
        }
    }
} 