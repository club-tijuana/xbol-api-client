using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class BundlePassRepository(XBOLDbContext dbContext) : BaseRepository<BundlePass>(dbContext)
    {
    }
}
