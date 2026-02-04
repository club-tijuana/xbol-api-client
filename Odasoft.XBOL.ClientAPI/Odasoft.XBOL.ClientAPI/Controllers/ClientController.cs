using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.Commons.Requests.Filters;
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

        [HttpPost("my-events")]
        [EndpointName("GetMyEventsAsync")]
        public async Task<ActionResult<PagedResponse<EventItemDTO>>> GetMyEventsAsync([FromBody] TicketsFilters filters)
        {
            long idClient = 1;

            var result = await _clientService.GetMyEventsAsync(filters, idClient);

            return Ok(result);
        }

        [HttpGet("my-event/{eventId}")]
        [EndpointName("GetMyEventDetailAsync")]
        public async Task<ActionResult<PagedResponse<EventItemDTO>>> GetMyEventDetailAsync(long eventId)
        {
            long idClient = 1;
            var result = await _clientService.GetMyEventDetailAsync(idClient, eventId);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPost("my-event-tickets")]
        [EndpointName("GetMyTicketsByOrderAsync")]
        public async Task<ActionResult<PagedResponse<EventItemDTO>>> GetMyTicketsByOrderAsync([FromBody] TicketsFilters filters)
        {
            var result = await _clientService.GetMyTicketsByOrderAsync(filters);

            return Ok(result);
        }
    }
}
