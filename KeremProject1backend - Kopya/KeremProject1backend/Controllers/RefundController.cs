using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using KeremProject1backend.Operations;
using KeremProject1backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
// using Microsoft.AspNetCore.Authorization; // Yetkilendirme için

namespace KeremProject1backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // TODO: Admin yetkilendirmesi ekle
    // [Authorize(Roles = "Admin,AdminAdmin")]
    public class RefundController : ControllerBase
    {
        private readonly GeneralContext _dbContext;
        private readonly UsersContext _usersContext;
        private readonly PayTrService _payTrService;
        private readonly ILogger<RefundController> _logger;

        public RefundController(
            GeneralContext dbContext,
            UsersContext usersContext,
            PayTrService payTrService,
            ILogger<RefundController> logger)
        {
            _dbContext = dbContext;
            _usersContext = usersContext;
            _payTrService = payTrService;
            _logger = logger;
        }

        // Direkt İade Talebi Oluşturma Endpoint'i (Admin)
        // Bu metot, kullanıcı talebi olmadan direkt PayTR'a iade yapar.
        [HttpPost("RequestRefund")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> RequestRefund([FromBody] RefundRequest request, [FromHeader] string token)
        {
            var result = await RefundOperations.RequestRefundOperation(token, request, _dbContext, _payTrService, _usersContext, _logger);
            return Ok(result);
        }

        // --- YENİ: Admin İade Yönetimi Endpointleri ---

        // Onay Bekleyen İade Taleplerini Listele
        [HttpGet("PendingRequests")]
        [Produces("application/json", Type = typeof(BaseResponse))] // List<ReturnRequestSummaryResponse> BaseResponse.Response içinde olacak
        public async Task<IActionResult> GetPendingReturnRequests([FromHeader] string token)
        {
             var result = await RefundOperations.GetPendingReturnRequestsOperation(token, _dbContext, _usersContext, _logger);
             return Ok(result);
        }

        // İade Talebini Onayla
        [HttpPost("Approve/{orderId}")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> ApproveReturnRequest(int orderId, [FromBody] ProcessReturnRequest request, [FromHeader] string token)
        {
            var result = await RefundOperations.ApproveReturnRequestOperation(token, orderId, request, _dbContext, _payTrService, _usersContext, _logger);
             return Ok(result);
        }

         // İade Talebini Reddet
        [HttpPost("Reject/{orderId}")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> RejectReturnRequest(int orderId, [FromBody] ProcessReturnRequest request, [FromHeader] string token)
        {
            var result = await RefundOperations.RejectReturnRequestOperation(token, orderId, request, _dbContext, _usersContext, _logger);
             return Ok(result);
        }
    }
} 