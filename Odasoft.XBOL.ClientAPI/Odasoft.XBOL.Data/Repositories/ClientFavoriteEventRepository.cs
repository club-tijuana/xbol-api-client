using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class ClientFavoriteEventRepository(XBOLDbContext dbContext) : BaseRepository<ClientFavoriteEvent>(dbContext)
    {
    }
}
