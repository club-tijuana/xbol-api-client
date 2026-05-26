using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.ClientAPI.Services;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/clients")]
    [ApiController]
    public class ClientController : ControllerBase
    {
        private readonly ClientService _clientService;
        private readonly IClientIdentityService _clientIdentityService;

        public ClientController(ClientService clientService, IClientIdentityService clientIdentityService)
        {
            _clientService = clientService;
            _clientIdentityService = clientIdentityService;
        }

        /// <summary>
        /// Retrieves a paginated list of events associated with the user filtered by order type.
        /// </summary>
        /// <param name="page">The page number to retrieve. If not specified, the first page is returned.</param>
        /// <param name="pageSize">The number of events per page. If not specified, the default page size is used.</param>
        /// <param name="orderType">The order type used to filter the events.</param>
        /// <returns>A paginated list of events.</returns>
        [HttpGet("my-events")]
        [Authorize]
        [EndpointName("GetMyEventsAsync")]
        [ProducesResponseType(typeof(PagedResponse<MyEventDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<MyEventDTO>>> GetMyEventsAsync(
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] OrderType orderType)
        {
            var client = await _clientIdentityService.RequireCurrentClientAsync(User);
            var result = await _clientService.GetMyEventsAsync(page, pageSize, orderType, client.Id);

            return Ok(result);
        }

        [HttpGet("my-event/{eventId}/{orderId}")]
        [Authorize]
        [EndpointName("GetMyEventDetailAsync")]
        public async Task<ActionResult<MyEventDetailDTO>> GetMyEventDetailAsync(long eventId, long orderId)
        {
            var client = await _clientIdentityService.RequireCurrentClientAsync(User);
            var result = await _clientService.GetMyEventDetailAsync(client.Id, eventId, orderId);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        /// <summary>
        /// Retrieves a paginated list of tickets associated with a specific order and event.
        /// </summary>
        /// <param name="page">
        /// The page number to retrieve. If not specified, the first page is returned.
        /// </param>
        /// <param name="pageSize">
        /// The number of tickets per page. If not specified, the default page size is used.
        /// </param>
        /// <param name="eventId">
        /// The identifier of the event associated with the tickets.
        /// </param>
        /// <param name="orderId">
        /// The identifier of the order that contains the tickets.
        /// </param>
        /// <returns>A paginated response containing the tickets.</returns>
        [HttpGet("my-event-tickets")]
        [Authorize]
        [EndpointName("GetMyTicketsByOrderAsync")]
        [ProducesResponseType(typeof(PagedResponse<MyTicketDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<MyTicketDTO>>> GetMyTicketsByOrderAsync(
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] long eventId,
            [FromQuery] long orderId
        )
        {
            var client = await _clientIdentityService.RequireCurrentClientAsync(User);
            var result = await _clientService.GetMyTicketsByOrderAsync(page, pageSize, eventId, orderId, client.Id);

            return Ok(result);
        }
    }
}
