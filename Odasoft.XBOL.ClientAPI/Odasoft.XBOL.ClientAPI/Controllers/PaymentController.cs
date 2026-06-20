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
