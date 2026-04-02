using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Requests.Filters;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class EventSectionRepository(XBOLDbContext dbContext) : BaseRepository<EventSection>(dbContext)
    {
        public async Task<IList<ZoneDTO>> GetZonesByEventIdAsync(long scheduleId)
        {
            return await DbContext.Set<EventSection>()
                .Where(es => es.EventSchedule.Id == scheduleId)
                .Select(es => new ZoneDTO
                {
                    Id = es.BaseSection.BaseZoneId,
                    Name = es.BaseSection.BaseZone.Name
                })
                .Distinct()
                .ToListAsync();
        }

        public async Task<SeatAvailabilityDTO> GetSeatAvailabilityAsync(ReservationFilters filters)
        { // TODO: Merge the logic
            if (filters.SeasonId != null)
            {
                var query = DbContext.Set<SeasonSection>()
                    .Where(ss => ss.SeasonId == filters.SeasonId);

                if (filters.ZoneId != null)
                {
                    query = query.Where(ss => ss.BaseSection.BaseZoneId == filters.ZoneId);
                }

                decimal min = filters.PriceRange?.Min ?? 0;
                decimal? max = filters.PriceRange?.Max;

                var sections = await query
                    .Select(ss => new SectionDTO
                    {
                        Id = ss.Id,
                        Name = ss.BaseSection.Name,
                        DisplayName = ss.DisplayName,
                        Price =
                            (ss.Price >= min && (max == null || ss.Price <= max.Value))
                                ? ss.Price
                                : null
                    })
                    .ToListAsync();

                var seatOverrides = await query
                    .SelectMany(ss => ss.SeasonSeats)
                    .Where(seat =>
                        seat.PriceOverride != null &&
                        seat.PriceOverride >= min &&
                        (max == null || seat.PriceOverride <= max.Value)
                    )
                    .Select(seat => new SeatDTO
                    {
                        Id = seat.Id,
                        ExternalSeatObjectKey = seat.ExternalSeatObjectKey,
                        PriceOverride = seat.PriceOverride
                    })
                    .ToListAsync();

                return new SeatAvailabilityDTO
                {
                    Sections = sections,
                    SeatOverrides = seatOverrides
                };
            }
            else if (filters.ScheduleId != null)
            {
                var query = DbContext.Set<EventSection>()
                    .Where(es => es.EventScheduleId == filters.ScheduleId);

                if (filters.ZoneId != null)
                {
                    query = query.Where(es => es.BaseSection.BaseZoneId == filters.ZoneId);
                }

                decimal min = filters.PriceRange?.Min ?? 0;
                decimal? max = filters.PriceRange?.Max;

                var sections = await query
                    .Select(es => new SectionDTO
                    {
                        Id = es.Id,
                        Name = es.BaseSection.Name,
                        DisplayName = es.DisplayName,
                        Price =
                            (es.Price >= min && (max == null || es.Price <= max.Value))
                                ? es.Price
                                : null
                    })
                    .ToListAsync();

                var seatOverrides = await query
                    .SelectMany(es => es.EventSeats)
                    .Where(seat => seat.PriceOverride != null && seat.PriceOverride > 0)
                    .Select(seat => new SeatDTO
                    {
                        Id = seat.Id,
                        ExternalSeatObjectKey = seat.ExternalSeatObjectKey,
                        PriceOverride = seat.PriceOverride
                    })
                    .ToListAsync();

                return new SeatAvailabilityDTO
                {
                    Sections = sections,
                    SeatOverrides = seatOverrides
                };
            }
            else
            {
                return new SeatAvailabilityDTO();
            }
        }
    }
}
