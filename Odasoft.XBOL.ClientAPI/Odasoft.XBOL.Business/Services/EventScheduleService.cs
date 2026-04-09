using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Data.Repositories;
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
    }
}
