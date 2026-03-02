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
                    Category = e.Category,
                })
                .ToListAsync();

            return (mainEvents, mainEvents.Count);
        }

        public async Task<(List<EventItemDTO> Items, int TotalCount)> GetEventsAsync(EventsFilters filters)
        {
            var query = DbContext.Set<Models.Event>()
                .Where(e =>
                    e.Category == (filters.EventCategory != null ? filters.EventCategory : e.Category)
                );

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
                Category = e.Category
            })
            .ToListAsync();

            return (events, totalCount);
        }

        public async Task<EventDetailDTO?> GetEventDetailAsync(long eventId)
        {
            var query = DbContext.Set<Models.Event>()
                .Where(e => e.Id == eventId);

            EventDetailDTO? eventDetail = await query
                .Select(e => new EventDetailDTO
                {
                    Id = e.Id,
                    Image = e.PosterImageUrl,
                    Gallery = new List<string> { e.BannerImageUrl, e.PosterImageUrl },
                    Name = e.Name,
                    LongDescription = e.LongDescription,
                    ShortDescription = e.ShortDescription,
                    Schedules = e.Schedules.OrderBy(s => s.StartDateTime)
                        .Select(s => new EventScheduleDTO
                        {
                            Id = s.Id,
                            Date = s.StartDateTime,
                            Location = s.Event.VenueMap.Name
                        }).ToList(),
                    Category = e.Category
                })
                .FirstOrDefaultAsync();

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
                    Category = e.Event.Category,
                    EventKey = e.ExternalEventKey
                })
                .FirstAsync();

            return eventItem;
        }

        //public async Task<List<EventZoneAvailabilityDTO>> GetSeatsByScheduleIdAsync(ReservationFilters filters)
        //{
        //    return await DbContext.Set<EventSeat>()
        //        .Where(es => es.)
        //        .Select(es => new EventZoneAvailabilityDTO
        //        {
        //            ZoneId = es.BaseSection.BaseZoneId,
        //            ZoneName = es.BaseSection.BaseZone.Name
        //        })
        //        .Distinct()
        //        .ToListAsync();
        //}
    }
}
