using System;
using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using KeremProject1backend.Operations;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace KeremProject1backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private TestContext _testcontext;
        private UsersContext _usersContext;
        private readonly ILogger<TestController> _logger;

        public TestController(TestContext testcontext, UsersContext usersContext, ILogger<TestController> logger)
        {
            _testcontext = testcontext;
            _usersContext = usersContext;
            _logger = logger;
        }

        [HttpPost("test")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public IActionResult TestOperation([FromBody] TestRequest request, [FromHeader] string token)
        {
            return Ok(TestOperations.TestOperation(request, token, _testcontext, _usersContext));
        }

        [HttpGet("TestAuth")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public IActionResult TestAuth([FromHeader] string token)
        {
            return Ok(TestOperations.TestAuth(token, _usersContext));
        }
        
        [HttpGet("TestAdmin")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public IActionResult TestAdmin([FromHeader] string token)
        {
            return Ok(TestOperations.TestAdmin(token, _usersContext));
        }

        [HttpPost("CreateInitialAdmin")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> CreateInitialAdmin([FromBody] RegisterAdminRequest request)
        {
            var result = await TestOperations.CreateInitialAdminOperation(request, _usersContext, _logger);
            return Ok(result);
        }

    }
}
