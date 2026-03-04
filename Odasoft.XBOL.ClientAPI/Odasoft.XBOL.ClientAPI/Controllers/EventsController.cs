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

        [HttpPost("filtered-events")]
        [EndpointName("GetFilteredEventsAsync")]
        public async Task<ActionResult<FilteredEventsResponse<PerformerDTO, EventItemDTO>>> GetFilteredEventsAsync([FromBody] EventsFilters filters)
        {
            var result = await _eventService.GetFilteredEventsAsync(filters);

            return Ok(result);
        }

        [HttpGet("{eventId}")]
        [EndpointName("GetEventDetailAsync")]
        public async Task<ActionResult<EventDetailDTO>> GetEventDetailAsync([FromRoute] long eventId)
        {
            var result = await _eventService.GetEventDetailAsync(eventId);

            return Ok(result);
        }

        [HttpGet("categories")]
        [EndpointName("GetEventCategoriesAsync")]
        public async Task<ActionResult<EventCategoryDTO>> GetEventCategoriesAsync()
        {
            var result = await _eventService.GetEventCategories();

            return Ok(result);
        }
    }
}
