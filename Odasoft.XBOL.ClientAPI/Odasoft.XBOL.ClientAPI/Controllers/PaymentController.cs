using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business;
using Odasoft.XBOL.ClientAPI.Services;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ITicketingClient _ticketingClient;
        private readonly IClientIdentityService _clientIdentityService;

        public PaymentController(
            ITicketingClient ticketingClient,
            IClientIdentityService clientIdentityService)
        {
            _ticketingClient = ticketingClient;
            _clientIdentityService = clientIdentityService;
        }

        [AllowAnonymous]
        [HttpPost("initiate-checkout")]
        public async Task<ActionResult<InitiateCheckoutResponse>> InitiateCheckoutAsync(
            [FromBody] InitiateCheckoutRequest request)
        {
            var client = await _clientIdentityService.TryResolveCurrentClientAsync(User);

            if (request.RelatedOrderId.HasValue && client is null)
            {
                return Unauthorized("Renewal checkout requires an authenticated client.");
            }

            request.ClientContact.Id = client?.Id;

            return Ok(await _ticketingClient.InitiateCheckoutAsync(request));
        }

        [AllowAnonymous]
        [HttpPost("confirm-checkout")]
        public async Task<ActionResult<ConfirmCheckoutResponse>> ConfirmCheckoutAsync(
            [FromBody] ConfirmCheckoutRequest request)
        {
            return Ok(await _ticketingClient.ConfirmCheckoutAsync(request));
        }

        [AllowAnonymous]
        [HttpGet("is-order-paid/{orderRefId}")]
        public async Task<ActionResult<bool>> IsOrderPaidAsync([FromRoute] string orderRefId)
        {
            return Ok(await _ticketingClient.IsOrderPaidAsync(orderRefId));
        }
    }
}
