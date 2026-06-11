using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.Data.Extensions.Domain;
using Odasoft.XBOL.Data.Mapping;
using Odasoft.XBOL.Data.Queries;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;
using System.Data;

namespace Odasoft.XBOL.Data.Repositories
{
    public class EventRepository(XBOLDbContext dbContext) : BaseRepository<Event>(dbContext)
    {
        public async Task<(List<EventItemDTO> Items, int TotalCount)> GetMainEventsAsync(int pageSize, bool includeMedia = false)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var mainEvents = await DbContext.Set<Models.Event>()
                .Where(e =>
                    e.Schedules != null
                    && e.Schedules.All(es => es.OnSaleDate <= now && es.EndDateTime > now)
                    && e.Status == EventStatus.Published
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
                .OrderByDescending(e => e.Event.Schedules
                    .Where(s => s.StartDateTime > now)
                    .Min(s => s.StartDateTime)
                )
                .Take(pageSize)
                .Select(e => new
                {
                    Id = e.Event.Id,
                    Name = e.Event.Name,
                    StartDate = e.Event.Schedules
                        .OrderBy(s => s.StartDateTime)
                        .Select(s => s.StartDateTime)
                        .FirstOrDefault(),
                    Location = e.Event.VenueMap.Venue.Name,
                    Categories = e.Event.Categories
                        .Select(ec => new EventCategoryDTO
                        {
                            Id = ec.Id,
                            Name = ec.Name,
                            DisplayName = ec.DisplayName
                        }).ToList(),
                    BannerUrl = e.EventImages.Where(i => i.MediaType == ClientMediaType.Banner).OrderBy(i => i.Order).Select(i => i.BlobAsset.Url).FirstOrDefault(),
                    LegacyBannerUrl = e.Event.BannerImageUrl,
                    LegacyPosterUrl = e.Event.PosterImageUrl
                })
                .ToListAsync();

            var result = mainEvents.Select(e => new EventItemDTO
            {
                Id = e.Id,
                Name = e.Name,
                StartDate = e.StartDate,
                Location = e.Location,
                Categories = e.Categories,
                BannerImageUrl = e.BannerUrl != null
                    ? e.BannerUrl
                    : e.LegacyBannerUrl ?? string.Empty,
                PosterImageUrl = e.BannerUrl != null
                    ? e.BannerUrl
                    : e.LegacyPosterUrl ?? string.Empty
            }).ToList();

            await AttachEventMediaSetsAsync(result, includeMedia);

            return (result, result.Count);
        }

        public async Task<PagedResponse<EventItemDTO>> GetTrendingEventsAsync(
            int page,
            int pageSize,
            bool includeMedia = false)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var query = DbContext.Set<Models.Event>()
                .Where(e =>
                    e.Schedules != null
                    && e.Status == EventStatus.Published
                    && e.Schedules.All(es => es.OnSaleDate <= now && es.EndDateTime > now)
                    && e.ViewCount > 0
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
                .Take(pageSize)
                .Select(e => new
                {
                    Id = e.Event.Id,
                    Name = e.Event.Name,
                    StartDate = e.Event.Schedules
                        .OrderBy(s => s.StartDateTime)
                        .Select(s => s.StartDateTime)
                        .FirstOrDefault(),
                    Location = e.Event.VenueMap.Venue.Name,
                    Categories = e.Event.Categories
                        .Select(ec => new EventCategoryDTO
                        {
                            Id = ec.Id,
                            Name = ec.Name,
                            DisplayName = ec.DisplayName
                        }).ToList(),
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
                BannerImageUrl = e.BannerUrl != null
                    ? e.BannerUrl
                    : e.LegacyBannerUrl ?? string.Empty,
                PosterImageUrl = e.BannerUrl != null
                    ? e.BannerUrl
                    : e.LegacyPosterUrl ?? string.Empty
            }).ToList();

            await AttachEventMediaSetsAsync(result, includeMedia);

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
            bool includeMedia = false)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var query = DbContext.Set<Models.Event>()
                .Where(e =>
                    e.Schedules.Any(es =>
                        es.OnSaleDate <= now
                        && es.EndDateTime > now
                    ) &&
                    e.Status == EventStatus.Published
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
                Location = e.Event.VenueMap.Venue.Name,
                Categories = e.Event.Categories
                        .Select(ec => new EventCategoryDTO
                        {
                            Id = ec.Id,
                            Name = ec.Name,
                            DisplayName = ec.DisplayName
                        }).ToList(),
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
                BannerImageUrl = e.BannerUrl != null
                    ? e.BannerUrl
                    : e.LegacyBannerUrl ?? string.Empty,
                PosterImageUrl = e.BannerUrl != null
                    ? e.BannerUrl
                    : e.LegacyPosterUrl ?? string.Empty
            }).ToList();

            await AttachEventMediaSetsAsync(result, includeMedia);

            return new PagedResponse<EventItemDTO>
            {
                Items = result,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<PagedResponse<EventItemDTO>> GetUpcomingEventsAsync(
            int page,
            int pageSize)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var query = DbContext.Set<Models.Event>()
                .Where(e =>
                    e.Status == EventStatus.Published &&
                    e.Schedules.Any(es =>
                        es.Status != ScheduleStatus.Closed &&
                        es.Status != ScheduleStatus.Draft &&
                        (
                            es.PreSaleStartDate > now ||
                            es.OnSaleDate > now
                        )
                    )
                )
                .AsQueryable();

            int totalCount = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            var events = await query
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
                Location = e.Event.VenueMap.Venue.Name,
                Categories = e.Event.Categories
                        .Select(ec => new EventCategoryDTO
                        {
                            Id = ec.Id,
                            Name = ec.Name,
                            DisplayName = ec.DisplayName
                        }).ToList(),
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

        public async Task<EventDetailDTO?> GetEventDetailAsync(long eventId, bool includeImages = false, bool includeMedia = false)
        {
            var query = DbContext.Set<Models.Event>()
                .Where(e => e.Id == eventId);

            var eventEntity = await query
                .Include(e => e.VenueMap)
                    .ThenInclude(vm => vm.Venue)
                .Include(e => e.Schedules)
                    .ThenInclude(s => s.Sections)
                        .ThenInclude(sec => sec.BaseSection)
                .Include(e => e.Categories)
                .FirstOrDefaultAsync();

            if (eventEntity == null)
            {
                return null;
            }

            var eventImages = await DbContext.Set<Media>()
                .Include(m => m.BlobAsset)
                .AvailableBlobMedia()
                .Where(m => m.ReferenceType == ClientSaleType.Event && m.ReferenceId == eventId)
                .ToListAsync();

            var banner = eventImages
                .Where(i => i.MediaType == ClientMediaType.Banner)
                .OrderBy(i => i.Order)
                .FirstOrDefault();

            var gallery = eventImages
                .Where(i => i.MediaType == ClientMediaType.Gallery)
                .OrderBy(i => i.Order);

            var bannerUrl = banner?.Url;

            EventImagesDTO? images = null;
            if (includeImages)
            {
                images = new EventImagesDTO
                {
                    Horizontal = bannerUrl != null
                        ? bannerUrl
                        : eventEntity.BannerImageUrl,
                    Vertical = bannerUrl != null
                        ? bannerUrl
                        : eventEntity.PosterImageUrl
                };
            }

            var fallbackGallery = new[]
                {
                    bannerUrl,
                    eventEntity.BannerImageUrl,
                    eventEntity.PosterImageUrl
                }
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => url!)
                .Distinct()
                .ToList();

            EventDetailDTO? eventDetail = new EventDetailDTO
            {
                Id = eventEntity.Id,
                Image = bannerUrl != null
                    ? bannerUrl
                    : eventEntity.BannerImageUrl ?? string.Empty,
                Gallery = gallery != null && gallery.Any(i => i.Url != null)
                    ? gallery
                        .Where(i => i.Url != null)
                        .Select(i => i.Url!)
                        .ToList()
                    : fallbackGallery,
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
                            Location = s.Event.VenueMap.Venue.Name
                        }).ToList(),
                Categories = eventEntity.Categories
                        .Select(ec => new EventCategoryDTO
                        {
                            Id = ec.Id,
                            Name = ec.Name,
                            DisplayName = ec.DisplayName
                        }).ToList(),
                AgeRestriction = eventEntity.AgeRestriction,
                SecurityPolicies = eventEntity.SecurityPolicies,
                Images = images,
                Media = includeMedia
                    ? EventMediaSetMapper.CreateMediaSet(eventImages.Select(EventMediaSetMapper.CreateMediaResponse))
                    : null
            };

            return eventDetail;
        }

        private async Task AttachEventMediaSetsAsync(List<EventItemDTO> events, bool includeMedia)
        {
            if (!includeMedia || events.Count == 0)
            {
                return;
            }

            var mediaSets = await EventMediaSetMapper.GetEventMediaSetsAsync(DbContext, events.Select(e => e.Id).Distinct().ToList());

            foreach (var eventItem in events)
            {
                eventItem.Media = mediaSets.GetValueOrDefault(eventItem.Id) ?? new EventMediaSetResponse();
            }
        }
    }
}