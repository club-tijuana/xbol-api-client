using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class PaymentLinkRepository(XBOLDbContext dbContext) : BaseRepository<PaymentLink>(dbContext)
    {
    }
}
