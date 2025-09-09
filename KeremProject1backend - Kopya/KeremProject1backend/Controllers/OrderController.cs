using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using KeremProject1backend.Operations;
using KeremProject1backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeremProject1backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly GeneralContext _dbContext;
        private readonly PayTrService _payTrService;
        private readonly ShippingService _shippingService;
        private readonly ILogger<OrderController> _logger;
        private readonly UsersContext _usersContext;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly string? _callbackBaseUrl;

        public OrderController(
            GeneralContext dbContext,
            PayTrService payTrService,
            ShippingService shippingService,
            ILogger<OrderController> logger,
            UsersContext usersContext,
            EmailService emailService,
            IConfiguration configuration
        )
        {
            _dbContext = dbContext;
            _payTrService = payTrService;
            _shippingService = shippingService;
            _logger = logger;
            _usersContext = usersContext;
            _emailService = emailService;
            _configuration = configuration;
            _callbackBaseUrl = _configuration["AppSettings:CallbackBaseUrl"];
        }

        // Yeni: Sipariş Oluşturma
        [HttpPost("CreateOrder")]
        [Produces("application/json", Type = typeof(CreateOrderResponse))]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, [FromHeader] string? token)
        {
            var result = await OrderOperations.CreateOrderOperation(token, request, _dbContext, _usersContext, _logger, _emailService, _callbackBaseUrl);
            return Ok(result);
        }

        // Kullanıcının kendi siparişlerini listeler
        [HttpGet("GetUserOrders")]
        [Produces("application/json", Type = typeof(List<OrderSummaryResponse>))]
        public async Task<IActionResult> GetUserOrders([FromHeader] string token)
        {
            var result = await OrderOperations.GetUserOrdersOperation(token, _dbContext, _usersContext);
            return Ok(result);
        }

        // Admin için teslim edilmemiş siparişleri listeler
        [HttpPost("admin/nondelivered")]
        [Produces("application/json")]
        public async Task<IActionResult> GetNonDeliveredOrders([FromHeader] string token, [FromBody] GetNonDeliveredOrdersRequest request)
        {
            var result = await OrderOperations.GetNonDeliveredOrdersForAdminOperation(token, request, _dbContext, _usersContext, _logger);
            return Ok(result);
        }

        // Belirli bir siparişin detaylarını getirir
        [HttpGet("{orderId}")]
        [Produces("application/json", Type = typeof(OrderDetailsResponse))]
        public async Task<IActionResult> GetOrderDetails(int orderId, [FromHeader] string token)
        {
            var result = await OrderOperations.GetOrderDetailsOperation(token, orderId, _dbContext, _usersContext);
            return Ok(result);
        }

        // Siparişi iptal etme isteği
        [HttpPost("CancelOrder/{orderId}")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> CancelOrder(int orderId, [FromBody] CancelOrderRequest request, [FromHeader] string token)
        {
            var result = await OrderOperations.CancelOrderOperation(token, orderId, request, _dbContext, _usersContext, _payTrService, _logger);
            return Ok(result);
        }

        // İade talebi oluşturma
        [HttpPost("RequestReturn/{orderId}")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> RequestReturn(int orderId, [FromBody] ReturnRequest request, [FromHeader] string token)
        {
            var result = await OrderOperations.RequestReturnOperation(token, orderId, request, _dbContext, _usersContext, _logger);
            return Ok(result);
        }

        // Kargo bilgisi güncelleme (admin)
        [HttpPost("UpdateShipping/{orderId}")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> UpdateShippingInfo(int orderId, [FromBody] UpdateShippingInfoRequest request, [FromHeader] string token)
        {
            var result = await OrderOperations.UpdateShippingInfoOperation(token, orderId, request, _dbContext, _usersContext, _logger, _emailService, _callbackBaseUrl);
            return Ok(result);
        }

        // Sipariş durumu güncelleme (admin)
        [HttpPost("UpdateStatus/{orderId}")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request, [FromHeader] string token)
        {
            var result = await OrderOperations.UpdateOrderStatusOperation(token, orderId, request, _dbContext, _usersContext, _logger, _emailService, _callbackBaseUrl);
            return Ok(result);
        }

        // Siparişin kargo takip bilgisini getirir
        [HttpGet("{orderId}/tracking")]
        [Produces("application/json", Type = typeof(TrackingInfoResponse))]
        public async Task<IActionResult> GetTrackingInfo(int orderId, [FromHeader] string token)
        {
            var result = await OrderOperations.GetTrackingInfoOperation(token, orderId, _dbContext, _usersContext, _shippingService, _logger);
            return Ok(result);
        }

        // Kullanıcı için sipariş detaylarını getirir (kullanıcı dostu)
        [HttpGet("{orderId}/details")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> GetMyOrderDetails(int orderId, [FromHeader] string token)
        {
            var result = await OrderOperations.GetMyOrderDetailsOperation(token, orderId, _dbContext, _usersContext, _shippingService, _logger);
            return Ok(result);
        }

        // Yeni: Kargo Takip Numarası Yollama (Admin)
        [HttpPost("SendTrackingNumber")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> SendTrackingNumberToUser([FromBody] SendTrackingNumberRequest request, [FromHeader] string token)
        {
            var result = await OrderOperations.SendTrackingNumberToUserOperation(token, request, _dbContext, _usersContext, _logger, _emailService, _callbackBaseUrl);
            return Ok(result);
        }
    }
} 