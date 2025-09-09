using KeremProject1backend.Models.DBs;
using KeremProject1backend.Models.Requests;
using KeremProject1backend.Models.Responses;
using KeremProject1backend.Operations;
using Microsoft.AspNetCore.Mvc;

namespace KeremProject1backend.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private UsersContext _userscontext;
        public UserController(UsersContext userscontext)
        {
            _userscontext = userscontext;
        }

        [HttpPost("GeAllUser")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public IActionResult GetAllUser([FromBody] GetAllUserRequests request, [FromHeader] string token)
        {
            return Ok(UserOperations.GeAllUser(request, token, _userscontext));
        }
        [HttpPost("DeleteUser")]
        [Produces("application/json", Type = typeof(BaseResponse))]
        public IActionResult DeleteUser([FromBody] UserDeleteRequests request, [FromHeader] string token)
        {
            return Ok(UserOperations.DeleteUser(request, token, _userscontext));
        }
        [HttpPost("UpdateUser")]
        [Produces("application/json", Type = typeof(UpdateUserResponses))]
        public IActionResult UpdateUser([FromBody] UpdateUserRequests request, [FromHeader] string token)
        {
            return Ok(UserOperations.UpdateUser(request, token, _userscontext));
        }


    }
}
