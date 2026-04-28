using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class SeasonPassRepository(XBOLDbContext dbContext) : BaseRepository<SeasonPass>(dbContext)
    {
    }
}
