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
                //&& e.Schedules.All(es => es.StartDateTime > now) // TODO: Commented for testing purposes
                )
                .OrderByDescending(e => e.ViewCount)
                .AsQueryable();

            int totalCount = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            var events = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(e => new
                {
                    Id = e.Id,
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
                    IsFavorite = true,
                    BannerFile = e.EventImages.Where(i => i.ImageType == Commons.Enums.ImageType.HorizontalPoster).OrderBy(i => i.Order).FirstOrDefault(),
                    PosterFile = e.EventImages.Where(i => i.ImageType == Commons.Enums.ImageType.VerticalPoster).OrderBy(i => i.Order).FirstOrDefault(),
                    LegacyBannerUrl = e.BannerImageUrl,
                    LegacyPosterUrl = e.PosterImageUrl
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
                BannerImageUrl = e.BannerFile != null
                    ? $"data:{e.BannerFile.ContentType};base64,{Convert.ToBase64String(e.BannerFile.Content)}"
                    : e.LegacyBannerUrl ?? string.Empty,
                PosterImageUrl = e.PosterFile != null
                    ? $"data:{e.PosterFile.ContentType};base64,{Convert.ToBase64String(e.PosterFile.Content)}"
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
