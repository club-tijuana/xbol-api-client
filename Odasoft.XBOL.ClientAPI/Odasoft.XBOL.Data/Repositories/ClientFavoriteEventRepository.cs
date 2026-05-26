using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Enums;
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
                //&& e.Schedules.All(es => es.StartDateTime > now) // TODO: Commented for testing purposes
                )
                .GroupJoin(
                    DbContext.Set<Media>().Where(x => x.ReferenceType == ClientSaleType.Event && x.DeletedAt == null),
                    eventObject => eventObject.Id,
                    media => media.ReferenceId,
                    (e, m) => new
                    {
                        Event = e,
                        EventImages = m
                    }
                )
                .OrderByDescending(e => e.Event.ViewCount)
                .AsQueryable();

            int totalCount = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            var events = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(e => new
                {
                    Id = e.Event.Id,
                    Name = e.Event.Name,
                    StartDate = e.Event.Schedules
                        .OrderBy(s => s.StartDateTime)
                        .Select(s => s.StartDateTime)
                        .FirstOrDefault(),
                    Location = e.Event.VenueMap.Name,
                    Categories = e.Event.Categories
                        .Select(ec => new EventCategoryDTO
                        {
                            Id = ec.Id,
                            Name = ec.Name,
                            DisplayName = ec.DisplayName
                        }).ToList(),
                    IsFavorite = true,
                    BannerFile = e.EventImages.Where(i => i.MediaType == ClientMediaType.Banner).OrderBy(i => i.Order).FirstOrDefault(),
                    LegacyBannerUrl = e.Event.BannerImageUrl,
                    LegacyPosterUrl = e.Event.PosterImageUrl
                })
                .ToListAsync();

            var result = events.Select(e => new EventItemDTO
            {
                Id = e.Id,
                Name = e.Name,
                StartDate = e.StartDate,
                Location = e.Location,
                Categories = e.Categories,
                IsFavorite = e.IsFavorite,
                BannerImageUrl = e.BannerFile != null && e.BannerFile.Url != null
                    ? e.BannerFile.Url
                    : e.LegacyBannerUrl ?? string.Empty,
                PosterImageUrl = e.BannerFile != null && e.BannerFile.Url != null
                    ? e.BannerFile.Url
                    : e.LegacyPosterUrl ?? string.Empty
            }).ToList();

            return new PagedResponse<EventItemDTO>
            {
                Items = result,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
    }
}
