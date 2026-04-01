using Odasoft.XBOL.Business.Configs;
using Odasoft.XBOL.Commons.Requests;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Business.Services
{
    public class EventService
    {
        private readonly EventRepository _eventRepository;
        private readonly EventCategoryRepository _eventCategoryRepository;
        private readonly EventScheduleRepository _eventScheduleRepository;
        private readonly EventViewRepository _eventViewRepository;
        private readonly SearchSettings _searchSettings;
        private readonly EventsTrackingSettings _eventsTrackingSettings;

        private const int MIN_PAGE = 1;
        private const int MAX_PAGE = 50;
        private const int MAIN_PAGE_SIZE = 2;
        private const int MAIN_CURRENT_PAGE = 1;

        public EventService(
            EventRepository eventRepository,
            EventCategoryRepository eventCategoryRepository,
            EventScheduleRepository eventScheduleRepository,
            EventViewRepository eventViewRepository,
            SearchSettings searchSettings,
            EventsTrackingSettings eventsTrackingSettings
        )
        {
            _eventRepository = eventRepository;
            _eventCategoryRepository = eventCategoryRepository;
            _eventScheduleRepository = eventScheduleRepository;
            _eventViewRepository = eventViewRepository;
            _searchSettings = searchSettings;
            _eventsTrackingSettings = eventsTrackingSettings;
        }

        public async Task<PagedResponse<EventItemDTO>> GetMainEventsAsync(long? clientId)
        {
            (List<EventItemDTO> result, int totalCount) = await _eventRepository.GetMainEventsAsync(MAIN_PAGE_SIZE, clientId);

            return new PagedResponse<EventItemDTO>
            {
                Items = result,
                Page = MAIN_CURRENT_PAGE,
                PageSize = MAIN_PAGE_SIZE,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)MAIN_PAGE_SIZE)
            };
        }

        public async Task<PagedResponse<EventItemDTO>> GetTrendingEventsAsync(int? page, int? pageSize, long? clientId)
        {
            return await _eventRepository.GetTrendingEventsAsync(page ?? MIN_PAGE, pageSize ?? MAX_PAGE, clientId);
        }

        public async Task<PagedResponse<EventItemDTO>> GetEventsAsync(int? page, int? pageSize, long? eventCategoryId, string? searchTerm)
        {
            return await _eventRepository.GetEventsAsync(page ?? MIN_PAGE, pageSize ?? MAX_PAGE, eventCategoryId, searchTerm);
        }

        public async Task<FilteredEventsResponse<PerformerDTO, ScheduleItemDTO>> GetFilteredEventsAsync(
            int? page,
            int? pageSize,
            DateTimeOffset? rangeDateFrom,
            DateTimeOffset? rangeDateTo,
            string? searchTerm,
            long? performerId,
            List<long>? eventCategoryIds,
            bool? trendingEvents)
        {
            return await _eventScheduleRepository.GetFilteredEventsAsync(
                page ?? MIN_PAGE,
                pageSize ?? MAX_PAGE,
                rangeDateFrom,
                rangeDateTo,
                searchTerm,
                performerId,
                eventCategoryIds,
                trendingEvents,
                _searchSettings.MatchRatio);
        }

        public async Task<EventDetailDTO?> GetEventDetailAsync(long eventId, long? idClient)
        {
            return await _eventRepository.GetEventDetailAsync(eventId, idClient);
        }

        public async Task<List<EventCategoryDTO>> GetEventCategories()
        {
            return await _eventCategoryRepository.GetEventCategories();
        }

        public async Task RegisterView(EventViewRequest eventView)
        {
            if (eventView.EventId == null || string.IsNullOrEmpty(eventView.VisitorId))
            {
                return;
            }

            var ip = string.IsNullOrEmpty(eventView.IpAddress)
                ? "unknown"
                : eventView.IpAddress;

            try
            {
                await _eventViewRepository.TryRegisterViewAsync(
                    eventView.EventId.Value,
                    eventView.VisitorId,
                    ip,
                    eventView.Platform ?? "",
                    _eventsTrackingSettings.ViewDuplicateMinutes,
                    _eventsTrackingSettings.IpRateLimitMinutes,
                    _eventsTrackingSettings.MaxViewsPerIpPerMinute
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
