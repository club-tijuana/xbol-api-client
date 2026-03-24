using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Requests.Filters;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.Business.Services
{
    public class BookingService
    {
        private readonly EventSectionRepository _eventSectionRepository;
        private readonly EventScheduleRepository _eventScheduleRepository;
        private readonly SeasonRepository _seasonRepository;

        public BookingService(EventSectionRepository eventSectionRepository,
            EventScheduleRepository eventScheduleRepository,
            SeasonRepository seasonRepository)
        {
            _eventSectionRepository = eventSectionRepository;
            _eventScheduleRepository = eventScheduleRepository;
            _seasonRepository = seasonRepository;
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

        public async Task<SeasonItemDTO?> GetSeasonByIdAsync(long seasonId)
        {
            var now = DateTimeOffset.UtcNow;

            return await _seasonRepository.Get(
                    filter: season =>
                        season.Id == seasonId
                        && season.OnSaleDate <= now
                        && season.OffSaleDate >= now
                )
                .Select(season => new SeasonItemDTO
                {
                    Id = season.Id,
                    BannerImageUrl = season.BannerImageUrl,
                    StartDate = season.StartDate,
                    ExternalSeasonKey = season.ExternalSeasonKey
                })
                .FirstOrDefaultAsync();
        }
    }
}
