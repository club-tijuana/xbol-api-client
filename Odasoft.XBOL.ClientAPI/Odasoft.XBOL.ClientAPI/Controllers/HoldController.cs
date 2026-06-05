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

        [HttpPost("release")]
        [EndpointName("ReleaseHoldSeatsAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HoldToken))]
        public async Task<ActionResult<ICollection<string>?>> ReleaseHoldSeatsAsync([FromBody] ReleaseSeatsByKeyRequest request)
        {
            var result = await _bus.InvokeAsync<ICollection<string>?>(new ReleaseSeatsActionCommand(request));

            if (result is null)
            {
                return UnprocessableEntity("Unable to release the hold on the seats. It may have already expired or is invalid.");
            }

            return Ok(result);
        }

        [HttpPost("hold")]
        [EndpointName("HoldSeatsActionAsync")]
        [ProducesResponseType(typeof(HoldToken), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<HoldToken>> HoldSeatsActionAsync([FromBody] HoldSeatsActionRequest request)
        {
            var result = await _bus.InvokeAsync<HoldToken>(new HoldSeatsActionCommand(request));

            if (result is null)
            {
                return UnprocessableEntity("Unable to hold the selected seats. They may no longer be available.");
            }

            return Ok(result);
        }
    }
}
