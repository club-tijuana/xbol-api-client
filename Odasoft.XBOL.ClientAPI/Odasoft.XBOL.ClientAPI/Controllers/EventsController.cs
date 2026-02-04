using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.Commons.Requests.Filters;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/events")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly EventService _eventService;

        public EventsController(EventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet]
        [EndpointName("GetMainEventsAsync")]
        public async Task<ActionResult<PagedResponse<EventItemDTO>>> GetMainEventsAsync()
        {
            var result = await _eventService.GetMainEventsAsync();

            return Ok(result);
        }

        [HttpPost]
        [EndpointName("GetEventsAsync")]
        public async Task<ActionResult<PagedResponse<EventItemDTO>>> GetEventsAsync([FromBody] EventsFilters filters)
        {
            var result = await _eventService.GetEventsAsync(filters);

            return Ok(result);
        }
    }
}
