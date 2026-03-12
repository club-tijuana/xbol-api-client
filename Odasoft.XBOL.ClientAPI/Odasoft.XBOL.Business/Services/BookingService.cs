using Odasoft.XBOL.Commons.Requests.Filters;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.Business.Services
{
    public class BookingService
    {
        private readonly EventSectionRepository _eventSectionRepository;
        private readonly EventScheduleRepository _eventScheduleRepository;

        public BookingService(EventSectionRepository eventSectionRepository,
            EventScheduleRepository eventScheduleRepository)
        {
            _eventSectionRepository = eventSectionRepository;
            _eventScheduleRepository = eventScheduleRepository;
        }

        public async Task<IList<ZoneDTO>> GetZonesByEventIdAsync(long scheduleId)
        {
            return await _eventSectionRepository.GetZonesByEventIdAsync(scheduleId);
        }

        public async Task<IList<SectionDTO>> GetSeatAvailabilityAsync(ReservationFilters filters)
        {
            return await _eventSectionRepository.GetSeatAvailabilityAsync(filters);
        }

        public async Task<EventItemDTO> GetEventItemByScheduleIdAsync(long scheduleId)
        {
            return await _eventScheduleRepository.GetEventItemByScheduleIdAsync(scheduleId);
        }
    }
}
