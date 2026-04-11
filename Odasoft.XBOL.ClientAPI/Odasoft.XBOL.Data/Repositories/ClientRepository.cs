using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class ClientRepository(XBOLDbContext dbContext) : BaseRepository<Client>(dbContext)
    {
    }
}
