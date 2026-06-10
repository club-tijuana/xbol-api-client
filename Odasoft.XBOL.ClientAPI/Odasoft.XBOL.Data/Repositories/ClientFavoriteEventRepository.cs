using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.Data.Mapping;
using Odasoft.XBOL.Data.Queries;
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

        public async Task<PagedResponse<EventItemDTO>> GetFavoritesByClientIdAsync(int page, int pageSize, long clientId, bool includeMedia = false)
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
                    DbContext.Set<Media>().AvailableBlobMedia().Where(x => x.ReferenceType == ClientSaleType.Event),
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
                    BannerUrl = e.EventImages.Where(i => i.MediaType == ClientMediaType.Banner).OrderBy(i => i.Order).Select(i => i.BlobAsset.Url).FirstOrDefault(),
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
                BannerImageUrl = e.BannerUrl != null
                    ? e.BannerUrl
                    : e.LegacyBannerUrl ?? string.Empty,
                PosterImageUrl = e.BannerUrl != null
                    ? e.BannerUrl
                    : e.LegacyPosterUrl ?? string.Empty
            }).ToList();

            if (includeMedia && result.Count > 0)
            {
                var mediaSets = await EventMediaSetMapper.GetEventMediaSetsAsync(DbContext, result.Select(e => e.Id).Distinct().ToList());

                foreach (var eventItem in result)
                {
                    eventItem.Media = mediaSets.GetValueOrDefault(eventItem.Id) ?? new EventMediaSetResponse();
                }
            }

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
