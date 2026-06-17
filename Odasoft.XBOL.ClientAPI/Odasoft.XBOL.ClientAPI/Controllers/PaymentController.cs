using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ITicketingClient _ticketingClient;

        public PaymentController(ITicketingClient ticketingClient)
        {
            _ticketingClient = ticketingClient;
        }

        [AllowAnonymous]
        [HttpPost("create-session")]
        public async Task<ActionResult<SessionResponse>> CreateSessionAsync()
        {
            return Ok(await _ticketingClient.CreateSessionAsync());
        }

        [AllowAnonymous]
        [HttpPut("update-session/{sessionId}")]
        public async Task<ActionResult<SessionResponse>> UpdateSessionAsync([FromRoute] string sessionId, [FromBody] UpdateSessionRequest request)
        {
            return Ok(await _ticketingClient.UpdateSessionAsync(sessionId, request));
        }

        [AllowAnonymous]
        [HttpPost("initiate-checkout")]
        public async Task<ActionResult<InitiateCheckoutResponse>> InitiateCheckoutAsync(
            [FromBody] InitiateCheckoutRequest request)
        {
            return Ok(await _ticketingClient.InitiateCheckoutAsync(request));
        }

        [AllowAnonymous]
        [HttpPost("confirm-checkout")]
        public async Task<ActionResult<ConfirmCheckoutResponse>> ConfirmCheckoutAsync(
            [FromBody] ConfirmCheckoutRequest request)
        {
            return Ok(await _ticketingClient.ConfirmCheckoutAsync(request));
        }
    }
}
