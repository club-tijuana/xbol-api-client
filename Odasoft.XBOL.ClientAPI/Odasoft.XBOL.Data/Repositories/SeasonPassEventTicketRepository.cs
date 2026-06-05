using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class SeasonPassEventTicketRepository(XBOLDbContext dbContext) : BaseRepository<SeasonPassEventTicket>(dbContext)
    {
    }
}
