using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Business.Services
{
    public class EventScheduleService(EventScheduleRepository repository)
    {
        public async Task<long?> GetEventIdByExternalEventKeyAsync(string eventKey)
        {
            EventSchedule? schedule = await repository.Get(x => x.ExternalEventKey == eventKey).FirstOrDefaultAsync();

            return schedule?.EventId;
        }

        public async Task<EventSchedule?> GetEventScheduleByExternalEventKeyAsync(string eventKey)
        {
            EventSchedule? schedule = await repository.Get(x => x.ExternalEventKey == eventKey).FirstOrDefaultAsync();
            return schedule;
        }

        public async Task<SeoMetadataDTO?> GetEventMetadataByScheduleIdAsync(long scheduleId)
        {
            var evnt = await repository.Get(
                    filter: schedule => schedule.Id == scheduleId,
                    includedProperties: [
                        "Event"
                    ]
                )
                .Select(schedule => new SeoMetadataDTO
                {
                    Title = schedule.Event.Name,
                    Description = schedule.Event.ShortDescription,
                    ImageUrl = schedule.Event.PosterImageUrl
                })
                .FirstOrDefaultAsync();

            return evnt;
        }
    }
}
