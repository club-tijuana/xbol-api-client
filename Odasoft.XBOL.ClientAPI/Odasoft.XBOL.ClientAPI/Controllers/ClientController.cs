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
        [EndpointName("GetMyEventTicketsAsync")]
        public async Task<ActionResult<PagedResponse<EventItemDTO>>> GetMyEventTicketsAsync([FromBody] TicketsFilters filters)
        {
            long idClient = 1;

            var result = await _clientService.GetMyEventTicketsAsync(filters, idClient);

            return Ok(result);
        }

        [HttpPost("my-event-tickets")]
        [EndpointName("GetMyTicketsByEventAsync")]
        public async Task<ActionResult<PagedResponse<EventItemDTO>>> GetMyTicketsByEventAsync([FromBody] TicketsFilters filters)
        {
            long idClient = 1;

            var result = await _clientService.GetMyTicketsByEventAsync(filters, idClient);

            return Ok(result);
        }
    }
}
