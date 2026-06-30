using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class BundlePassEventTicketRepository(XBOLDbContext dbContext) : BaseRepository<BundlePassEventTicket>(dbContext)
    {
    }
}
