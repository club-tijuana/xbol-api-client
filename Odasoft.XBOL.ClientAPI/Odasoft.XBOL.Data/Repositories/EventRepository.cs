using FuzzySharp;
using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Requests.Filters;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;
using System.Data;

namespace Odasoft.XBOL.Data.Repositories
{
    public class EventRepository(XBOLDbContext dbContext) : BaseRepository<Event>(dbContext)
    {
        public async Task<(List<EventItemDTO> Items, int TotalCount)> GetMainEventsAsync()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            List<EventItemDTO> mainEvents = await DbContext.Set<Models.Event>()
                .Where(e =>
                    e.Schedules != null
                    && e.Schedules.All(es => es.StartDateTime > now)
                )
                .OrderByDescending(e => e.Schedules
                    .Where(s => s.StartDateTime > now)
                    .Min(s => s.StartDateTime)
                )
                .Take(2)
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
                })
                .ToListAsync();

            return (mainEvents, mainEvents.Count);
        }

        public async Task<(List<EventItemDTO> Items, int TotalCount)> GetTrendingEventsAsync(EventsFilters filters)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var query = DbContext.Set<Models.Event>()
                .Where(e =>
                    e.Schedules != null
                    && e.Schedules.All(es => es.StartDateTime > now)
                    && e.ViewCount > 0
                )
                .OrderByDescending(e => e.ViewCount)
                .AsQueryable();

            int totalCount = await query.CountAsync();
            var skip = (filters.Page - 1) * filters.PageSize;

            List<EventItemDTO> events = await query
                .Take(filters.PageSize)
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
                })
                .ToListAsync();

            return (events, totalCount);
        }

        public async Task<(List<EventItemDTO> Items, int TotalCount)> GetEventsAsync(EventsFilters filters)
        {
            var query = DbContext.Set<Models.Event>().AsQueryable();

            if (filters.EventCategoryId != null)
            {
                query = query
                    .Where(e => e.Categories
                        .Any(ec => ec.Id == filters.EventCategoryId)
                    );
            }

            if (!string.IsNullOrEmpty(filters.TextFilter))
            {
                query = query.
                    Where(e =>
                        e.Name.ToLower().Contains(filters.TextFilter.ToLower())
                        || e.VenueMap.Venue.Name.ToLower().Contains(filters.TextFilter.ToLower())
                    );
            }

            int totalCount = await query.CountAsync();
            var skip = (filters.Page - 1) * filters.PageSize;

            List<EventItemDTO> events = await query
            .Skip(skip)
            .Take(filters.PageSize)
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

            return (events, totalCount);
        }

        public async Task<(List<ScheduleItemDTO> Items, List<PerformerDTO> performers, int TotalCount)> GetFilteredEventsAsync(SearchEventsFilters filters, long matchRatio)
        {
            var query = DbContext.Set<Models.EventSchedule>()
                .Include(es => es.Event)
                .Where(es => es.StartDateTime >= DateTimeOffset.UtcNow)
                .AsQueryable();

            if (filters.PerformerId != null)
            {
                query = query
                    .Where(es => es.Event.PerformerId == filters.PerformerId);
            }

            if (filters.EventCategoryIds != null && filters.EventCategoryIds.Any())
            {
                query = query
                    .Where(es => es.Event.Categories
                        .Any(c => filters.EventCategoryIds.Contains(c.Id))
                    );
            }

            if (!string.IsNullOrEmpty(filters.TextFilter))
            {
                var text = filters.TextFilter.ToLower();

                query = query
                    .Where(es =>
                        es.Event.Name.ToLower().Contains(text)
                        || es.Event.VenueMap.Venue.Name.ToLower().Contains(text)
                    );
            }

            if (filters.RangeDateFrom != null)
            {
                DateTime from = filters.RangeDateFrom.Value.Date;
                query = query
                    .Where(es => es.StartDateTime.Date >= from);
            }

            if (filters.RangeDateTo != null)
            {
                DateTime to = filters.RangeDateTo.Value.Date.AddDays(1);
                query = query
                    .Where(es => es.StartDateTime.Date < to);
            }

            if (filters.TrendingEvents != null && filters.TrendingEvents == true)
            {
                query = query
                    .Where(es => es.Event.ViewCount > 0)
                    .OrderByDescending(es => es.Event.ViewCount);
            }

            int totalCount = await query.CountAsync();
            var skip = (filters.Page - 1) * filters.PageSize;

            List<ScheduleItemDTO> events = await query
            .Skip(skip)
            .Take(filters.PageSize)
            .Select(es => new ScheduleItemDTO
            {
                Id = es.Id,
                StartDate = es.StartDateTime,
                Event = new EventItemDTO
                {
                    Id = es.EventId,
                    BannerImageUrl = es.Event.BannerImageUrl,
                    PosterImageUrl = es.Event.PosterImageUrl,
                    Name = es.Event.Name,
                    StartDate = es.StartDateTime,
                    Location = es.Event.VenueMap.Venue.Name,
                    Categories = es.Event.Categories
                        .Select(ec => new EventCategoryDTO
                        {
                            Id = ec.Id,
                            Name = ec.Name,
                            DisplayName = ec.DisplayName
                        })
                        .ToList()
                }
            })
            .ToListAsync();

            var performerQuery = DbContext.Set<Performer>().AsQueryable();

            List<Performer> performers = new List<Performer>();
            List<PerformerDTO> performersDto = new List<PerformerDTO>();

            if (filters.PerformerId != null)
            {
                performersDto = await performerQuery
                    .Where(p => p.Id == filters.PerformerId)
                    .Select(p => new PerformerDTO
                    {
                        Id = p.Id,
                        Name = p.Name,
                        ImageUrl = p.ImageUrl,
                    })
                    .ToListAsync();
            }
            else if (!string.IsNullOrEmpty(filters.TextFilter))
            {
                string filter = filters.TextFilter;

                performers = await performerQuery
                    .Where(p => p.IsActive)
                    .ToListAsync();

                performersDto = performers
                    .Where(p => Fuzz.TokenSetRatio(p.Name, filter) >= matchRatio)
                    .Select(p => new PerformerDTO
                    {
                        Id = p.Id,
                        Name = p.Name,
                        ImageUrl = p.ImageUrl,
                    })
                    .ToList();
            }

            return (events, performersDto, totalCount);
        }

        public async Task<EventDetailDTO?> GetEventDetailAsync(long eventId)
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
            };

            return eventDetail;
        }

        public async Task<List<ZoneDTO>> GetZonesByEventIdAsync(long scheduleId)
        {
            return await DbContext.Set<EventSection>()
                .Where(es => es.EventSchedule.Id == scheduleId)
                .Select(es => new ZoneDTO
                {
                    Id = es.BaseSection.BaseZoneId,
                    Name = es.BaseSection.BaseZone.Name
                })
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<SectionDTO>> GetSeatAvailabilityAsync(ReservationFilters filters)
        {
            var query = DbContext.Set<EventSection>()
                .Where(es => es.EventScheduleId == filters.ScheduleId);

            if (filters.PriceRange != null)
            {
                decimal min = filters.PriceRange.Min == null ? 0 : filters.PriceRange.Min.Value;
                decimal? max = filters.PriceRange.Max;

                query = query.Where(es =>
                    es.Price >= min
                    && es.Price <= (max == null ? es.Price : max.Value)
                );
            }

            if (filters.ZoneId != null)
            {
                query = query.Where(es => es.BaseSection.BaseZoneId == filters.ZoneId);
            }

            return await query.Select(es => new SectionDTO
            {
                Id = es.Id,
                Name = es.BaseSection.Name,
                DisplayName = es.DisplayName,
                Price = es.Price
            })
            .ToListAsync();
        }

        public async Task<EventItemDTO> GetEventItemByScheduleIdAsync(long scheduleId)
        {
            EventItemDTO eventItem = await DbContext.Set<EventSchedule>()
                .Where(es => es.Id == scheduleId)
                .Select(e => new EventItemDTO
                {
                    Id = e.Id,
                    BannerImageUrl = e.Event.BannerImageUrl,
                    PosterImageUrl = e.Event.PosterImageUrl,
                    Name = e.Event.Name,
                    StartDate = e.StartDateTime,
                    Location = e.Event.VenueMap.Name,
                    Categories = e.Event.Categories
                        .Select(ec => new EventCategoryDTO
                        {
                            Id = ec.Id,
                            Name = ec.Name,
                            DisplayName = ec.DisplayName
                        }).ToList(),
                    EventKey = e.ExternalEventKey
                })
                .FirstAsync();

            return eventItem;
        }

        public async Task<List<EventCategoryDTO>> GetEventCategories()
        {
            List<EventCategoryDTO> categories = await DbContext.Set<EventCategory>()
                .Where(ec => ec.IsActive)
                .Select(ec => new EventCategoryDTO
                {
                    Id = ec.Id,
                    Name = ec.Name,
                    DisplayName = ec.DisplayName
                })
                .ToListAsync();

            return categories;
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
