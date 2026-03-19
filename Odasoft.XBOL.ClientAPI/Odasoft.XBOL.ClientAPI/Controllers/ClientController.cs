using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business.Services;
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

        public ClientController(ClientService clientService)
        {
            _clientService = clientService;
        }

        /// <summary>
        /// Retrieves a paginated list of events associated with the user filtered by order type.
        /// </summary>
        /// <param name="page">The page number to retrieve. If not specified, the first page is returned.</param>
        /// <param name="pageSize">The number of events per page. If not specified, the default page size is used.</param>
        /// <param name="orderType">The order type used to filter the events.</param>
        /// <returns>A paginated list of events.</returns>
        [HttpGet("my-events")]
        [EndpointName("GetMyEventsAsync")]
        [ProducesResponseType(typeof(PagedResponse<MyEventDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<MyEventDTO>>> GetMyEventsAsync(
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] OrderType orderType)
        {
            // TODO: Remove temp token
            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                long idClient = token == "TEST-TOKEN" ? 1 : 2;

                var result = await _clientService.GetMyEventsAsync(page, pageSize, orderType, idClient);

                return Ok(result);
            }
            else
            {
                return Unauthorized();
            }
        }

        [HttpGet("my-event/{eventId}")]
        [EndpointName("GetMyEventDetailAsync")]
        public async Task<ActionResult<MyEventDetailDTO>> GetMyEventDetailAsync(long eventId)
        {
            // TODO: Remove temp token
            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                long idClient = token == "TEST-TOKEN" ? 1 : 2;

                var result = await _clientService.GetMyEventDetailAsync(idClient, eventId);

                if (result == null)
                {
                    return NotFound();
                }

                return Ok(result);
            }
            else
            {
                return Unauthorized();
            }
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
        [EndpointName("GetMyTicketsByOrderAsync")]
        [ProducesResponseType(typeof(PagedResponse<MyTicketDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<MyTicketDTO>>> GetMyTicketsByOrderAsync(
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] long eventId,
            [FromQuery] long orderId
        )
        {
            // TODO: Remove temp token
            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                long idClient = token == "TEST-TOKEN" ? 1 : 2;

                var result = await _clientService.GetMyTicketsByOrderAsync(page, pageSize, eventId, orderId, idClient);

                return Ok(result);
            }
            else
            {
                return Unauthorized();
            }
        }
    }
}
