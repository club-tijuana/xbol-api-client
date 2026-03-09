using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Odasoft.XBOL.Business.Configs;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.Commons.Requests;
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
        private readonly IMemoryCache _memoryCache;
        private readonly EventsTrackingSettings _eventsTrackingSettings;

        public EventsController(EventService eventService, IMemoryCache memoryCache, EventsTrackingSettings eventsTrackingSettings)
        {
            _eventService = eventService;
            _memoryCache = memoryCache;
            _eventsTrackingSettings = eventsTrackingSettings;
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
        public async Task<ActionResult<FilteredEventsResponse<PerformerDTO, ScheduleItemDTO>>> GetFilteredEventsAsync([FromBody] EventsFilters filters)
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

        [HttpPost("view")]
        [EndpointName("RegisterEventViewAsync")]
        public async Task<ActionResult> RegisterView([FromBody] EventViewRequest request)
        {
            string ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                   ?? HttpContext.Connection.RemoteIpAddress?.ToString()
                   ?? "unknown";

            var key = $"rate:{ip}";
            var count = _memoryCache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return 0;
            });

            if (count >= _eventsTrackingSettings.MaxViewsPerIpPerMinute)
                return StatusCode(429, "Too many requests");

            _memoryCache.Set(key, count + 1, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            });

            request.IpAddress = ip;

            await _eventService.RegisterView(request);

            return Ok();
        }
    }
}
