using Odasoft.XBOL.Data.Repositories;

namespace Odasoft.XBOL.Business.Services
{
    public class EventScheduleService
    {
        private readonly EventScheduleRepository _eventScheduleRepository;

        public EventScheduleService(EventScheduleRepository eventScheduleRepository)
        {
            _eventScheduleRepository = eventScheduleRepository;
        }


    }
}
