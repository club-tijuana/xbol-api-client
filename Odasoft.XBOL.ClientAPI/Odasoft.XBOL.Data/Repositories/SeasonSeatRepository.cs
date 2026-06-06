using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class SeasonSeatRepository(XBOLDbContext dbContext) : BaseRepository<SeasonSeat>(dbContext)
    {
        public async Task<List<SeatDTO>?> GetSeasonSeatPricesAsync(long seasonId, List<string> seats)
        {
            var prevSeasonSeats = await DbContext.Set<SeasonSeat>()
                .Include(s => s.SeasonSection)
                .Where(seat =>
                    seat.SeasonSection.SeasonId == seasonId &&
                    seats.Contains(seat.ExternalSeatObjectKey)
                )
                .Select(seat => new SeatDTO
                {
                    Id = seat.Id,
                    ExternalSeatObjectKey = seat.ExternalSeatObjectKey,
                    PriceOverride = 500
                })
                .ToListAsync();

            prevSeasonSeats = prevSeasonSeats
                .GroupBy(x => x.ExternalSeatObjectKey)
                .Select(g => g.First())
                .ToList();

            return prevSeasonSeats;
        }
    }
}
