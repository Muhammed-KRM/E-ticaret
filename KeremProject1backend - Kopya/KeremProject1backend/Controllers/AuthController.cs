using System;
using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using KeremProject1backend.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace KeremProject1backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private UsersContext _usersContext;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UsersContext usersContext, ILogger<AuthController> logger)
        {
            _usersContext = usersContext;
            _logger = logger;
        }

        [HttpPost("Login")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await AuthOperations.LoginOperation(request, _usersContext, _logger);
            return Ok(result);
        }

        [HttpPost("SignUp")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public IActionResult SignUp([FromBody] SignUpRequest request)
        {
            var result = AuthOperations.SignUpOperation(request, _usersContext);
            return Ok(result);
        }

        [HttpPost("RegisterAdmin")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminRequest request, [FromHeader] string token)
        {
            var result = await AuthOperations.RegisterAdminOperation(token, request, _usersContext, _logger);
            return Ok(result);
        }
        
        [HttpPost("UpdatePassword")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request, [FromHeader] string token)
        {
            var result = await AuthOperations.UpdatePasswordOperation(token, request, _usersContext, _logger);
            return Ok(result);
        }
        
        [HttpGet("GetMyInfo")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public IActionResult GetMyInfo([FromHeader] string token)
        {
            var result = AuthOperations.GetMyInfoOperation(token, _usersContext);
            return Ok(result);
        }

        [HttpPost("Logout")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public IActionResult Logout([FromHeader] string token)
        {
            var result = AuthOperations.LogoutOperation(token, _usersContext);
            return Ok(result);
        }

        [HttpPost("UpdateMyProfile")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileRequest request, [FromHeader] string token)
        {
            var result = await UserOperations.UpdateMyProfileOperation(request, token, _usersContext, _logger);
            return Ok(result);
        }

    }
}
