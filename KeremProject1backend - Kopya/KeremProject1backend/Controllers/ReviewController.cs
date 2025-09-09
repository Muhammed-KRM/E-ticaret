using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using KeremProject1backend.Operations;
using KeremProject1backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace KeremProject1backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly GeneralContext _dbContext;
        private readonly UsersContext _usersContext;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(
            GeneralContext dbContext,
            UsersContext usersContext,
            ILogger<ReviewController> logger
            )
        {
            _dbContext = dbContext;
            _usersContext = usersContext;
            _logger = logger;
        }

        // Belirli bir ürünün yorumlarını getirir
        [HttpGet("GetProductReviews/{productId}")]
        [AllowAnonymous] // Yorumları herkes görebilir
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> GetProductReviews(int productId)
        {
            var result = await ReviewOperations.GetProductReviewsOperation(productId, _dbContext, _logger);
            return Ok(result);
        }

        // Yeni bir yorum ekler
        [HttpPost("AddReview")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> AddReview([FromBody] AddReviewRequest request, [FromHeader] string token)
        {
            var result = await ReviewOperations.AddReviewOperation(token, request, _dbContext, _usersContext, _logger);
            return Ok(result);
        }

        // Yorum Silme Endpoint'i (Admin)
        [HttpDelete("{reviewId}")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> DeleteReview(int reviewId, [FromHeader] string token)
        {
            var result = await ReviewOperations.DeleteReviewOperation(token, reviewId, _dbContext, _usersContext, _logger);
            return Ok(result);
        }

        // --- Admin Endpointleri (Yorum onayı/silme vb.) ---
        // Örnek:
        // [HttpPut("{reviewId}/approve")]
        // [Authorize(Roles = "Admin,AdminAdmin")]
        // public async Task<IActionResult> ApproveReview(int reviewId)
        // {
        //      // ReviewOperations içinde onaylama metodu çağrılmalı
        // }
    }
} 