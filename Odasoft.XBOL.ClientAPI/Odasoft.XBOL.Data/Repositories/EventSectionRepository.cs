using Microsoft.EntityFrameworkCore;
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

        public async Task<IList<ZoneDTO>> GetZonesBySeasonIdAsync(long seasonId)
        {
            return await DbContext.Set<SeasonSection>()
                .Where(ss => ss.SeasonId == seasonId)
                .Select(ss => new ZoneDTO
                {
                    Id = ss.BaseSection.BaseZoneId,
                    Name = ss.BaseSection.BaseZone.Name
                })
                .Distinct()
                .ToListAsync();
        }
    }
}