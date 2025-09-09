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
    public class GenerelController : ControllerBase
    {
        private readonly GeneralContext _generalContext;
        private readonly UsersContext _usersContext;
        private readonly ILogger<GenerelController> _logger;

        public GenerelController(GeneralContext generalContext, UsersContext usersContext, ILogger<GenerelController> logger)
        {
            _generalContext = generalContext;
            _usersContext = usersContext;
            _logger = logger;
        }

        [HttpGet("GetContactInfo")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> GetContactInfo()
        {
            return Ok(await GeneralOperations.GetContactInfoOperation(_generalContext, _logger));
        }

        [HttpPut("UpdateContactInfo")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> UpdateContactInfo([FromBody] UpdateContactRequest request, [FromHeader] string token)
        {
            return Ok(await GeneralOperations.UpdateContactInfoOperation(token, request, _generalContext, _usersContext, _logger));
        }

        [HttpPost("GetSystemLogs")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> GetSystemLogs([FromBody] GetSystemLogsRequest? request, [FromHeader] string token)
        {
            return Ok(await GeneralOperations.GetSystemLogsOperation(token, request ?? new GetSystemLogsRequest(), _usersContext, _logger));
        }

        [HttpPost("SendReport")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> SendReport([FromBody] SubmitReportRequest request, [FromHeader] string token)
        {
            return Ok(await GeneralOperations.SubmitReportOperation(token, request, _generalContext, _usersContext, _logger));
        }

        [HttpPost("GetReports")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public async Task<IActionResult> GetReports([FromHeader] string token)
        {
            return Ok(await GeneralOperations.GetReportsOperation(token, _usersContext, _generalContext, _logger));
        }

        [HttpPost("GetAllConfigurationData")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public IActionResult GetAllConfigurationData([FromBody] GetAllConfigurationDataRequest request, [FromHeader] string token)
        {
            return Ok(GeneralOperations.GetAllConfigurationDataOperation(request, token, _usersContext));
        }
    }
}
