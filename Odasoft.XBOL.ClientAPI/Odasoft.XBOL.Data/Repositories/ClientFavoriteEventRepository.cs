using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class ClientFavoriteEventRepository(XBOLDbContext dbContext) : BaseRepository<ClientFavoriteEvent>(dbContext)
    {
        public async Task InsertRangeAsync(IEnumerable<ClientFavoriteEvent> entities)
        {
            await DbSet.AddRangeAsync(entities);
        }

        public async Task<PagedResponse<EventItemDTO>> GetFavoritesByClientIdAsync(int page, int pageSize, long clientId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var query = DbContext.Set<Models.ClientFavoriteEvent>()
                .Where(f => f.ClientId == clientId)
                .Select(f => f.Event)
                .Where(e =>
                    e.Schedules != null
                    && e.Schedules.All(es => es.StartDateTime > now)
                    && e.ViewCount > 0
                )
                .OrderByDescending(e => e.ViewCount)
                .AsQueryable();

            int totalCount = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            List<EventItemDTO> events = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(e => new EventItemDTO
                {
                    Id = e.Id,
                    BannerImageUrl = e.BannerImageUrl,
                    PosterImageUrl = e.PosterImageUrl,
                    Name = e.Name,
                    StartDate = e.Schedules
                        .OrderBy(s => s.StartDateTime)
                        .Select(s => s.StartDateTime)
                        .FirstOrDefault(),
                    Location = e.VenueMap.Name,
                    Categories = e.Categories
                        .Select(ec => new EventCategoryDTO
                        {
                            Id = ec.Id,
                            Name = ec.Name,
                            DisplayName = ec.DisplayName
                        }).ToList(),
                    IsFavorite = true
                })
                .ToListAsync();

            return new PagedResponse<EventItemDTO>
            {
                Items = events,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
    }
}
