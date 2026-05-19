using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class MediaRepository(XBOLDbContext dbContext) : BaseRepository<Media>(dbContext)
    {
    }
}
