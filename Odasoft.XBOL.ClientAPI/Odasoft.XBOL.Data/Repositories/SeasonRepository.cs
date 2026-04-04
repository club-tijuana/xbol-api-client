using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class SeasonRepository(XBOLDbContext dbContext) : BaseRepository<Season>(dbContext)
    {
        public async Task<long?> GetSeasonIdByExternalSeasonKeyAsync(string seasonKey)
        {
            var season = await DbContext.Set<Season>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ExternalSeasonKey == seasonKey);

            return season?.Id;
        }
    }
}
