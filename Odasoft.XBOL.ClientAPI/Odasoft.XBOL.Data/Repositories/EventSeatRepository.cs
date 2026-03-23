using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class EventSeatRepository(XBOLDbContext dbContext) : BaseRepository<EventSeat>(dbContext)
    {
    }
}
