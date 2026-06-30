using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class BundleRepository(XBOLDbContext dbContext) : BaseRepository<Bundle>(dbContext)
    {

    }
}
