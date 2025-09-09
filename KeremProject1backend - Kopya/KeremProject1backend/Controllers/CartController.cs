using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using KeremProject1backend.Operations;
// using Microsoft.AspNetCore.Authorization; // Kaldırıldı
using Microsoft.AspNetCore.Mvc;
// using System.Security.Claims; // Kaldırıldı
using System.Threading.Tasks;
// using Microsoft.AspNetCore.Http; // Kaldırıldı (IHttpContextAccessor yerine token)
using System.Collections.Generic;

namespace KeremProject1backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Kaldırıldı
    public class CartController : ControllerBase
    {
        private readonly GeneralContext _dbContext;
        // private readonly IHttpContextAccessor _httpContextAccessor; // Kaldırıldı
        private readonly UsersContext _usersContext; // Eklendi (SessionService için gerekebilir)

        public CartController(GeneralContext dbContext, UsersContext usersContext /* Eklendi */)
        {
            _dbContext = dbContext;
            _usersContext = usersContext; // Eklendi
        }

        // GetUserId() metodu kaldırıldı

        // Sepete ürün ekle
        [HttpPost("AddToCart")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request, [FromHeader] string? token)
        {
            var result = await CartOperations.AddToCartOperation(token, request, _dbContext, _usersContext);
            return Ok(result);
        }

        // Sepetten ürün kaldır
        [HttpDelete("RemoveFromCart/{cartItemId}")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> RemoveFromCart(int cartItemId, [FromHeader] string? token, [FromQuery] string? guestCartId = null)
        {
            var result = await CartOperations.RemoveFromCartOperation(token, cartItemId, guestCartId, _dbContext, _usersContext);
            return Ok(result);
        }

        // Sepeti getir
        [HttpGet("GetCart")]
        [Produces("application/json", Type = typeof(KeremProject1backend.Models.Responses.CartResponse))]
        public async Task<IActionResult> GetCart([FromHeader] string? token, [FromQuery] string? guestCartId = null)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await CartOperations.GetCartOperation(token, guestCartId, _dbContext, _usersContext, baseUrl);
            return Ok(result);
        }

        [HttpPost("UpdateToCart")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> UpdateCart([FromBody] UpdateCartRequest request, [FromHeader] string? token)
        {
            var result = await CartOperations.UpdateCartOperation(token, request.CartItemId, request.NewQuantity, request.GuestCartId, _dbContext, _usersContext);
            return Ok(result);
        }
    }
} 