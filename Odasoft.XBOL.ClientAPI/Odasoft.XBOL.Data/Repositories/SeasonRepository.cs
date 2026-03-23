using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class SeasonRepository(XBOLDbContext dbContext) : BaseRepository<Season>(dbContext)
    {
    }
}
