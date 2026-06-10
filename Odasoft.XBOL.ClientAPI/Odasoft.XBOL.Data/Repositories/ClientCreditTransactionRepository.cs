using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class ClientCreditTransactionRepository(XBOLDbContext dbContext) : BaseRepository<ClientCreditTransaction>(dbContext)
    {
    }
}
