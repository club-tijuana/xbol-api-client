using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.ClientAPI.Services;
using Odasoft.XBOL.DTO.Requests;
using Odasoft.XBOL.DTO.Responses;

namespace Odasoft.XBOL.ClientAPI.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController(IClientIdentityService clientIdentityService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterResponse>> RegisterAsync(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await clientIdentityService.RegisterAsync(request, User, cancellationToken));
    }

    [Authorize]
    [HttpPost("complete-login")]
    [ProducesResponseType(typeof(AuthMeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthMeResponse>> CompleteLoginAsync(CancellationToken cancellationToken)
    {
        return Ok(await clientIdentityService.CompleteLoginAsync(User, cancellationToken));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthMeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthMeResponse>> GetMeAsync()
    {
        return Ok(await clientIdentityService.GetMeAsync(User));
    }

}
