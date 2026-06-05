using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Odasoft.XBOL.Business.Configs;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.ClientAPI.Services;
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
        private readonly EventScheduleService _eventScheduleService;
        private readonly IMemoryCache _memoryCache;
        private readonly EventsTrackingSettings _eventsTrackingSettings;
        private readonly IClientIdentityService _clientIdentityService;

        public EventsController(
            EventService eventService,
            EventScheduleService eventScheduleService,
            IMemoryCache memoryCache,
            EventsTrackingSettings eventsTrackingSettings,
            IClientIdentityService clientIdentityService)
        {
            _eventService = eventService;
            _eventScheduleService = eventScheduleService;
            _memoryCache = memoryCache;
            _eventsTrackingSettings = eventsTrackingSettings;
            _clientIdentityService = clientIdentityService;
        }

        [HttpGet("main")]
        [EndpointName("GetMainEventsAsync")]
        public async Task<ActionResult<PagedResponse<EventItemDTO>>> GetMainEventsAsync(
            [FromQuery] bool includeMedia = false)
        {
            var result = await _eventService.GetMainEventsAsync(includeMedia);

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
            [FromQuery] int? pageSize,
            [FromQuery] bool includeMedia = false)
        {
            var result = await _eventService.GetTrendingEventsAsync(page, pageSize, includeMedia);

            return Ok(result);
        }

        [HttpGet]
        [EndpointName("GetEventsAsync")]
        [ProducesResponseType(typeof(PagedResponse<EventItemDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<EventItemDTO>>> GetEventsAsync(
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] long? eventCategoryId,
            [FromQuery] string? searchTerm,
            [FromQuery] bool includeMedia = false)
        {
            var result = await _eventService.GetEventsAsync(page, pageSize, eventCategoryId, searchTerm, includeMedia);

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
            [FromQuery] bool? trendingEvents,
            [FromQuery] bool includeMedia = false)
        {
            var result = await _eventService.GetFilteredEventsAsync(
                page,
                pageSize,
                rangeDateFrom,
                rangeDateTo,
                searchTerm,
                performerId,
                eventCategoryIds,
                trendingEvents,
                includeMedia);

            return Ok(result);
        }

        /// <summary>
        /// Retrieves the full detail for a single event.
        /// </summary>
        /// <param name="eventId">Event identifier.</param>
        /// <param name="includeImages">When true, populates the legacy <c>images</c> map. Default false to keep the response small.</param>
        /// <param name="includeMedia">When true, populates the read-only <c>media</c> object from available shared media rows.</param>
        [HttpGet("{eventId}")]
        [EndpointName("GetEventDetailAsync")]
        [ProducesResponseType(typeof(EventDetailDTO), StatusCodes.Status200OK)]
        public async Task<ActionResult<EventDetailDTO>> GetEventDetailAsync(
            [FromRoute] long eventId,
            [FromQuery] bool includeImages = false,
            [FromQuery] bool includeMedia = false)
        {
            var result = await _eventService.GetEventDetailAsync(eventId, includeImages, includeMedia);

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
            {
                return StatusCode(429, "Too many requests");
            }

            _memoryCache.Set(key, count + 1, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            });

            request.IpAddress = ip;

            await _eventService.RegisterView(request);

            return Ok();
        }

        [HttpGet("{scheduleId}/metadata")]
        [EndpointName("GetEventMetadataByScheduleIdAsync")]
        [ProducesResponseType(typeof(SeoMetadataDTO), StatusCodes.Status200OK)]
        public async Task<ActionResult<SeoMetadataDTO?>> GetEventMetadataByScheduleIdAsync(
            [FromRoute] long scheduleId)
        {
            var result = await _eventScheduleService.GetEventMetadataByScheduleIdAsync(scheduleId);

            return Ok(result);
        }

        [HttpGet("upcoming-events")]
        [EndpointName("GetUpcomingEventsAsync")]
        [ProducesResponseType(typeof(PagedResponse<EventItemDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<EventItemDTO>>> GetUpcomingEventsAsync(
            [FromQuery] int? page,
            [FromQuery] int? pageSize)
        {
            var result = await _eventService.GetUpcomingEventsAsync(page, pageSize);

            return Ok(result);
        }
    }
}
