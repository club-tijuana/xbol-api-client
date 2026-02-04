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
                .Where(e => e.Schedules != null && e.Schedules.All(es => es.StartDateTime > now))
                .OrderByDescending(e => e.Id)
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
                        e.Name.Contains(filters.TextFilter)
                        || e.VenueMap.Venue.Name.Contains(filters.TextFilter)
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
    }
}
