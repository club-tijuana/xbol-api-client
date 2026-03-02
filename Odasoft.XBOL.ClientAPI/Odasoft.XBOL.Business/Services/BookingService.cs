using Odasoft.XBOL.Commons.Requests.Filters;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.Business.Services
{
    public class BookingService
    {
        private readonly EventRepository _eventRepository;

        public BookingService(EventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<List<ZoneDTO>> GetZonesByEventIdAsync(long scheduleId)
        {
            return await _eventRepository.GetZonesByEventIdAsync(scheduleId);
        }

        public async Task<List<SectionDTO>> GetSeatAvailabilityAsync(ReservationFilters filters)
        {
            return await _eventRepository.GetSeatAvailabilityAsync(filters);
        }

        public async Task<EventItemDTO> GetEventItemByScheduleIdAsync(long scheduleId)
        {
            return await _eventRepository.GetEventItemByScheduleIdAsync(scheduleId);
        }
    }
}
