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

        public async Task<IList<SectionDTO>> GetSeatAvailabilityAsync(ReservationFilters filters)
        {
            var query = DbContext.Set<EventSection>()
                .Where(es => es.EventScheduleId == filters.ScheduleId);

            if (filters.PriceRange != null)
            {
                decimal min = filters.PriceRange.Min == null ? 0 : filters.PriceRange.Min.Value;
                decimal? max = filters.PriceRange.Max;

                query = query.Where(es =>
                    es.Price >= min
                    && es.Price <= (max == null ? es.Price : max.Value)
                );
            }

            if (filters.ZoneId != null)
            {
                query = query.Where(es => es.BaseSection.BaseZoneId == filters.ZoneId);
            }

            return await query.Select(es => new SectionDTO
            {
                Id = es.Id,
                Name = es.BaseSection.Name,
                DisplayName = es.DisplayName,
                Price = es.Price
            })
            .ToListAsync();
        }
    }
}
