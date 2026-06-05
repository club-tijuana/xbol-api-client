using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class PaymentRepository(XBOLDbContext dbContext) : BaseRepository<Payment>(dbContext)
    {
    }
}
