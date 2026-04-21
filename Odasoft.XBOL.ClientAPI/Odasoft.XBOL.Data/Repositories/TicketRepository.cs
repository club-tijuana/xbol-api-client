using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class TicketRepository(XBOLDbContext dbContext) : BaseRepository<Ticket>(dbContext)
    {
    }
}
