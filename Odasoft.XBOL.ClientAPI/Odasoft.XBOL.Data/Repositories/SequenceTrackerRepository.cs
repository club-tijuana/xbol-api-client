using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class SequenceTrackerRepository(XBOLDbContext dbContext) : BaseRepository<SequenceTracker>(dbContext)
    {
    }
}
