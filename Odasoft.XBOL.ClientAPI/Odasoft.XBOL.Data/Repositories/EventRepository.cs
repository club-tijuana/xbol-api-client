using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Responses;
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

            List<EventItemDTO> mainEvents = await DbContext.Set<Models.Event>()
                .Where(e =>
                    e.Schedules != null
                    && e.Schedules.All(es => es.StartDateTime > now)
                    && e.Categories.Any(ec => ec.Id == 2 || ec.Id == 3)
                )
                .OrderByDescending(e => e.Schedules
                    .Where(s => s.StartDateTime > now)
                    .Min(s => s.StartDateTime)
                )
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
                    IsFavorite = clientId != null && favoriteEventIds.Contains(e.Id)
                })
                .ToListAsync();

            return (mainEvents, mainEvents.Count);
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
                    && e.Schedules.All(es => es.StartDateTime > now)
                    && e.ViewCount > 0
                )
                .OrderByDescending(e => e.ViewCount)
                .AsQueryable();

            int totalCount = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            List<EventItemDTO> events = await query
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
                    IsFavorite = clientId != null && favoriteEventIds.Contains(e.Id)
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

        public async Task<PagedResponse<EventItemDTO>> GetEventsAsync(
            int page,
            int pageSize,
            long? eventCategoryId,
            string? searchTerm)
        {
            var query = DbContext.Set<Models.Event>().AsQueryable();

            if (eventCategoryId != null)
            {
                query = query
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
                Location = e.VenueMap.Venue.Name,
                Categories = e.Categories
                        .Select(ec => new EventCategoryDTO
                        {
                            Id = ec.Id,
                            Name = ec.Name,
                            DisplayName = ec.DisplayName
                        }).ToList(),
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

        public async Task<EventDetailDTO?> GetEventDetailAsync(long eventId, long? clientId)
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

            EventDetailDTO? eventDetail = new EventDetailDTO
            {
                Id = eventEntity.Id,
                Image = eventEntity.PosterImageUrl,
                Gallery = new List<string> { eventEntity.BannerImageUrl, eventEntity.PosterImageUrl },
                Name = eventEntity.Name,
                LongDescription = eventEntity.LongDescription,
                ShortDescription = eventEntity.ShortDescription,
                AddressLine = eventEntity.VenueMap.Venue.AddressLine,
                City = eventEntity.VenueMap.Venue.City,
                State = eventEntity.VenueMap.Venue.State,
                Country = eventEntity.VenueMap.Venue.Country,
                ZipCode = eventEntity.VenueMap.Venue.ZipCode,
                FullAddress = GetFullAddress(eventEntity.VenueMap.Venue),
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
                IsFavorite = isFavorite
            };

            return eventDetail;
        }

        private static string GetFullAddress(Venue venue)
        {
            return string.Join(", ", new[]
            {
                venue.Name,
                venue.AddressLine,
                venue.ZipCode,
                venue.City,
                venue.State,
                venue.Country
            }.Where(s => !string.IsNullOrWhiteSpace(s)));
        }
    }
}
