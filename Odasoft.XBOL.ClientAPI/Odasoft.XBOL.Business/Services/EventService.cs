using Odasoft.XBOL.Business.Configs;
using Odasoft.XBOL.Commons.Requests;
using Odasoft.XBOL.Commons.Requests.Filters;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.Business.Services
{
    public class EventService
    {
        private readonly EventRepository _eventRepository;
        private readonly EventViewRepository _eventViewRepository;
        private readonly SearchSettings _searchSettings;
        private readonly EventsTrackingSettings _eventsTrackingSettings;

        public EventService(
            EventRepository eventRepository,
            EventViewRepository eventViewRepository,
            SearchSettings searchSettings,
            EventsTrackingSettings eventsTrackingSettings
        )
        {
            _eventRepository = eventRepository;
            _eventViewRepository = eventViewRepository;
            _searchSettings = searchSettings;
            _eventsTrackingSettings = eventsTrackingSettings;
        }

        public async Task<PagedResponse<EventItemDTO>> GetMainEventsAsync()
        {
            (List<EventItemDTO> result, int totalCount) = await _eventRepository.GetMainEventsAsync();

            return new PagedResponse<EventItemDTO>
            {
                Items = result,
                CurrentPage = 1,
                PageSize = 1,
                TotalItems = totalCount,
                TotalPages = 1
            };
        }

        public async Task<PagedResponse<EventItemDTO>> GetEventsAsync(EventsFilters filters)
        {
            filters.Page = Math.Max(filters.Page, 1);
            filters.PageSize = Math.Clamp(filters.PageSize, 1, 50);

            (List<EventItemDTO> result, int totalCount) = await _eventRepository.GetEventsAsync(filters);

            return new PagedResponse<EventItemDTO>
            {
                Items = result,
                CurrentPage = filters.Page,
                PageSize = filters.PageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filters.PageSize)
            };
        }

        public async Task<FilteredEventsResponse<PerformerDTO, ScheduleItemDTO>> GetFilteredEventsAsync(EventsFilters filters)
        {
            filters.Page = Math.Max(filters.Page, 1);
            filters.PageSize = Math.Clamp(filters.PageSize, 1, 50);

            (List<ScheduleItemDTO> result, List<PerformerDTO> performers, int totalCount) = await _eventRepository.GetFilteredEventsAsync(filters, _searchSettings.MatchRatio);

            return new FilteredEventsResponse<PerformerDTO, ScheduleItemDTO>
            {
                Performers = performers,
                PagedEvents = new PagedResponse<ScheduleItemDTO>
                {
                    Items = result,
                    CurrentPage = filters.Page,
                    PageSize = filters.PageSize,
                    TotalItems = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filters.PageSize)
                }
            };
        }

        public async Task<EventDetailDTO?> GetEventDetailAsync(long eventId)
        {
            return await _eventRepository.GetEventDetailAsync(eventId);
        }

        public async Task<List<EventCategoryDTO>> GetEventCategories()
        {
            return await _eventRepository.GetEventCategories();
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
