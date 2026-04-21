using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business;
using Odasoft.XBOL.Business.Messages;
using Wolverine;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/hold-seats")]
    [ApiController]
    public class HoldController : ControllerBase
    {
        private readonly IMessageBus _bus;

        public HoldController(IMessageBus bus)
        {
            _bus = bus;
        }

        [HttpPost("hold-token")]
        [EndpointName("HoldSeatsAsync")]
        [ProducesResponseType(typeof(HoldToken), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<HoldToken>> HoldSeatsAsync()
        {
            var result = await _bus.InvokeAsync<HoldToken>(new HoldTokenCommand());

            if (result is null)
            {
                return UnprocessableEntity("Unable to hold the selected seats. They may no longer be available.");
            }

            return Ok(result);
        }

        [HttpDelete]
        [EndpointName("ReleaseHoldSeatsAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HoldToken))]
        public async Task<ActionResult<HoldToken>> ReleaseHoldSeatsAsync([FromQuery] string holdToken)
        {
            var result = await _bus.InvokeAsync<HoldToken>(new ReleaseHoldSeatsCommand(holdToken));

            if (result is null)
            {
                return UnprocessableEntity("Unable to release the hold on the seats. It may have already expired or is invalid.");
            }

            return Ok(result);
        }
    }
}
