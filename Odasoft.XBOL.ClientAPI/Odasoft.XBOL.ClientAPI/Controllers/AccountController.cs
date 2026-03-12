using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.ClientAPI.Configs;
using Odasoft.XBOL.Commons.Security;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private Authentication _authentication;

        public AccountController(Authentication authentication)
        {
            _authentication = authentication;
        }

        [HttpPost("sign-in")]
        [AllowAnonymous]
        public async Task<IActionResult> Authenticate([FromBody] User model)
        {
            if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest();
            }

            bool auth = _authentication.AllowedUsers.Any(user =>
                string.Equals(user.Email, model.Username, StringComparison.OrdinalIgnoreCase)
                && user.Password == model.Password);

            if (auth)
            {
                return Ok(new User
                {
                    UserId = new Guid("019c29aa-cd4b-7407-9b65-d5d49891eb04"),
                    FirstName = "User",
                    LastName = "Test",
                    Username = model.Username,
                    Token = "TEST-TOKEN"
                });
            }
            else
            {
                return Unauthorized();
            }
        }
    }
}
