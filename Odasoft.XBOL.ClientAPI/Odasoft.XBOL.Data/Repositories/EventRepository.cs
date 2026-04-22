using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.Data.Extensions.Domain;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;
using System.Data;

namespace Odasoft.XBOL.Data.Repositories
{
    public class EventRepository(XBOLDbContext dbContext) : BaseRepository<Event>(dbContext)
    {
        public async Task<(List<EventItemDTO> Items, int TotalCount)> GetMainEventsAsync(int pageSize, long? clientId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var favoriteEventIds = clientId != null
                ? await DbContext.Set<Models.ClientFavoriteEvent>()
                    .Where(f => f.ClientId == clientId)
                    .Select(f => f.EventId)
                    .ToListAsync() : new List<long>();

            var mainEvents = await DbContext.Set<Models.Event>()
                .Where(e =>
                    e.Schedules != null
                    && e.Schedules.All(es => es.OnSaleDate <= now && es.EndDateTime > now)
                )
                .OrderByDescending(e => e.Schedules
                    .Where(s => s.StartDateTime > now)
                    .Min(s => s.StartDateTime)
                )
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
                    IsFavorite = clientId != null && favoriteEventIds.Contains(e.Id),
                    BannerFile = e.EventImages.Where(i => i.ImageType == Commons.Enums.ImageType.HorizontalPoster).OrderBy(i => i.Order).FirstOrDefault(),
                    PosterFile = e.EventImages.Where(i => i.ImageType == Commons.Enums.ImageType.VerticalPoster).OrderBy(i => i.Order).FirstOrDefault(),
                    LegacyBannerUrl = e.BannerImageUrl,
                    LegacyPosterUrl = e.PosterImageUrl
                })
                .ToListAsync();

            var result = mainEvents.Select(e => new EventItemDTO
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

            return (result, result.Count);
        }

        public async Task<PagedResponse<EventItemDTO>> GetTrendingEventsAsync(
            int page,
            int pageSize, long? clientId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var favoriteEventIds = clientId != null
               ? await DbContext.Set<Models.ClientFavoriteEvent>()
                   .Where(f => f.ClientId == clientId)
                   .Select(f => f.EventId)
                   .ToListAsync() : new List<long>();

            var query = DbContext.Set<Models.Event>()
                .Where(e =>
                    e.Schedules != null
                    && e.Schedules.All(es => es.OnSaleDate <= now && es.EndDateTime > now)
                    && e.ViewCount > 0
                )
                .OrderByDescending(e => e.ViewCount)
                .AsQueryable();

            int totalCount = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            var events = await query
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
                    IsFavorite = clientId != null && favoriteEventIds.Contains(e.Id),
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

        public async Task<PagedResponse<EventItemDTO>> GetEventsAsync(
            int page,
            int pageSize,
            long? eventCategoryId,
            string? searchTerm,
            long? clientId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            List<long> favouriteEventIds = new List<long>();
            if (clientId != null)
            {
                favouriteEventIds = await DbContext.Set<ClientFavoriteEvent>().AsQueryable()
                    .Where(c => c.ClientId == clientId)
                    .Select(c => c.EventId)
                    .ToListAsync();
            }

            var query = DbContext.Set<Models.Event>()
                .Include(e => e.EventImages)
                .Where(e =>
                    e.Schedules.Any(es =>
                        es.OnSaleDate <= now
                        && es.EndDateTime > now
                    )
                )
                .AsQueryable();

            if (eventCategoryId != null)
            {
                query = query
                    .Include(e => e.Categories)
                    .Where(e => e.Categories
                        .Any(ec => ec.Id == eventCategoryId)
                    );
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.
                    Where(e =>
                        e.Name.ToLower().Contains(searchTerm.ToLower())
                        || e.VenueMap.Venue.Name.ToLower().Contains(searchTerm.ToLower())
                    );
            }

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
                Location = e.VenueMap.Venue.Name,
                Categories = e.Categories
                        .Select(ec => new EventCategoryDTO
                        {
                            Id = ec.Id,
                            Name = ec.Name,
                            DisplayName = ec.DisplayName
                        }).ToList(),
                IsFavorite = favouriteEventIds.Contains(e.Id),
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

        public async Task<EventDetailDTO?> GetEventDetailAsync(long eventId, long? clientId, bool includeImages = false)
        {
            bool isFavorite = false;

            var query = DbContext.Set<Models.Event>()
                .Where(e => e.Id == eventId);

            var eventEntity = await query
                .Include(e => e.VenueMap)
                    .ThenInclude(vm => vm.Venue)
                .Include(e => e.Schedules)
                    .ThenInclude(s => s.Sections)
                        .ThenInclude(sec => sec.BaseSection)
                .Include(e => e.Categories)
                .Include(e => e.EventImages)
                .FirstOrDefaultAsync();

            if (eventEntity == null)
            {
                return null;
            }
            if (clientId != null)
            {
                isFavorite = await DbContext.Set<Models.ClientFavoriteEvent>()
                    .AnyAsync(c => c.ClientId == clientId && c.EventId == eventId);
            }

            var banner = eventEntity.EventImages
                .Where(i => i.ImageType == Commons.Enums.ImageType.HorizontalPoster)
                .OrderBy(i => i.Order)
                .FirstOrDefault();

            EventImagesDTO? images = null;
            if (includeImages)
            {
                var horizontal = eventEntity.EventImages
                    .Where(i => i.ImageType == Commons.Enums.ImageType.HorizontalPoster)
                    .OrderBy(i => i.Order)
                    .FirstOrDefault();

                var vertical = eventEntity.EventImages
                    .Where(i => i.ImageType == Commons.Enums.ImageType.VerticalPoster)
                    .OrderBy(i => i.Order)
                    .FirstOrDefault();

                images = new EventImagesDTO
                {
                    Horizontal = horizontal != null
                        ? $"data:{horizontal.ContentType};base64,{Convert.ToBase64String(horizontal.Content)}"
                        : eventEntity.BannerImageUrl,
                    Vertical = vertical != null
                        ? $"data:{vertical.ContentType};base64,{Convert.ToBase64String(vertical.Content)}"
                        : eventEntity.PosterImageUrl
                };
            }

            EventDetailDTO? eventDetail = new EventDetailDTO
            {
                Id = eventEntity.Id,
                Image = banner != null
                    ? $"data:{banner.ContentType};base64,{Convert.ToBase64String(banner.Content)}"
                    : eventEntity.BannerImageUrl ?? string.Empty,
                Gallery = eventEntity.EventImages
                    .Where(i => i.ImageType == Commons.Enums.ImageType.Gallery)
                    .OrderBy(i => i.Order)
                    .Select(i => $"data:{i.ContentType};base64,{Convert.ToBase64String(i.Content)}")
                    .ToList(),
                Name = eventEntity.Name,
                LongDescription = eventEntity.LongDescription,
                ShortDescription = eventEntity.ShortDescription,
                City = eventEntity.VenueMap.Venue.City,
                State = eventEntity.VenueMap.Venue.State,
                Country = eventEntity.VenueMap.Venue.Country,
                ZipCode = eventEntity.VenueMap.Venue.ZipCode,
                FullAddress = eventEntity.VenueMap.Venue.GetFullAddress(),
                Latitude = eventEntity.VenueMap.Venue.Latitude,
                Longitude = eventEntity.VenueMap.Venue.Longitude,
                Schedules = eventEntity.Schedules.OrderBy(s => s.StartDateTime)
                        .Select(s => new EventScheduleDTO
                        {
                            Id = s.Id,
                            Date = s.StartDateTime,
                            Location = s.Event.VenueMap.Name,
                            SectionPrices = s.Sections // TODO: Mapper
                                .GroupBy(es => es.Price)
                                .Select(g => new EventScheduleSectionPricesDTO
                                {
                                    Price = g.Key,
                                    Objects = g.Select(es => es.BaseSection.Name).ToList(),
                                    Currency = "MXN", // TODO: Add currency support for totals
                                })
                                .ToList()
                        }).ToList(),
                Categories = eventEntity.Categories
                        .Select(ec => new EventCategoryDTO
                        {
                            Id = ec.Id,
                            Name = ec.Name,
                            DisplayName = ec.DisplayName
                        }).ToList(),
                IsFavorite = isFavorite,
                AgeRestriction = eventEntity.AgeRestriction,
                SecurityPolicies = eventEntity.SecurityPolicies,
                Images = images
            };

            return eventDetail;
        }
    }
}