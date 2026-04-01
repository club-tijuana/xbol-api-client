using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Odasoft.XBOL.Business.Configs;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.Commons.Requests;
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

        [HttpGet("main")]
        [EndpointName("GetMainEventsAsync")]
        public async Task<ActionResult<PagedResponse<EventItemDTO>>> GetMainEventsAsync()
        {
            // TODO: Remove temp token
            long? clientId = null;

            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                clientId = token == "TEST-TOKEN" ? 1 : 2;

            }

            var result = await _eventService.GetMainEventsAsync(clientId);

            return Ok(result);
        }

        /// <summary>
        /// Retrieves a paginated list of trending events.
        /// </summary>
        /// <param name="page">
        /// The page number to retrieve. If not specified, the first page is returned.
        /// </param>
        /// <param name="pageSize">
        /// The number of events per page. If not specified, the default page size is used.
        /// </param>
        /// <returns>A paginated response containing trending events.</returns>
        [HttpGet("trending-events")]
        [EndpointName("GetTrendingEventsAsync")]
        [ProducesResponseType(typeof(PagedResponse<EventItemDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<EventItemDTO>>> GetTrendingEventsAsync(
            [FromQuery] int? page,
            [FromQuery] int? pageSize)
        {
            // TODO: Remove temp token
            long? clientId = null;

            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                clientId = token == "TEST-TOKEN" ? 1 : 2;

            }

            var result = await _eventService.GetTrendingEventsAsync(page, pageSize, clientId);

            return Ok(result);
        }

        [HttpGet]
        [EndpointName("GetEventsAsync")]
        [ProducesResponseType(typeof(PagedResponse<EventItemDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<EventItemDTO>>> GetEventsAsync(
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] long? eventCategoryId,
            [FromQuery] string? searchTerm)
        {
            // TODO: Remove temp token
            long? clientId = null;

            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                clientId = token == "TEST-TOKEN" ? 1 : 2;

            }

            var result = await _eventService.GetEventsAsync(page, pageSize, eventCategoryId, searchTerm, clientId);

            return Ok(result);
        }

        [HttpGet("filtered-events")]
        [EndpointName("GetFilteredEventsAsync")]
        [ProducesResponseType(typeof(FilteredEventsResponse<PerformerDTO, ScheduleItemDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<FilteredEventsResponse<PerformerDTO, ScheduleItemDTO>>> GetFilteredEventsAsync(
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] DateTimeOffset? rangeDateFrom,
            [FromQuery] DateTimeOffset? rangeDateTo,
            [FromQuery] string? searchTerm,
            [FromQuery] long? performerId,
            [FromQuery] List<long>? eventCategoryIds,
            [FromQuery] bool? trendingEvents)
        {
            var result = await _eventService.GetFilteredEventsAsync(
                page,
                pageSize,
                rangeDateFrom,
                rangeDateTo,
                searchTerm,
                performerId,
                eventCategoryIds,
                trendingEvents);

            return Ok(result);
        }

        [HttpGet("{eventId}")]
        [EndpointName("GetEventDetailAsync")]
        public async Task<ActionResult<EventDetailDTO>> GetEventDetailAsync([FromRoute] long eventId)
        {
            // TODO: Remove temp token
            long? idClient = null;

            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                idClient = token == "TEST-TOKEN" ? 1 : 2;

            }

            var result = await _eventService.GetEventDetailAsync(eventId, idClient);

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
