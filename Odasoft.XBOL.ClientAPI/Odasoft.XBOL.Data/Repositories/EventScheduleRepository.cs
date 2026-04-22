using FuzzySharp;
using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class EventScheduleRepository(XBOLDbContext dbContext) : BaseRepository<EventSchedule>(dbContext)
    {
        public async Task<EventItemDTO?> GetEventItemByScheduleIdAsync(long scheduleId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var eventItem = await DbContext.Set<EventSchedule>()
                .Where(es =>
                    es.Id == scheduleId
                    && es.OffSaleDate > now
                    && (
                        (es.PreSaleStartDate <= now && now < es.OnSaleDate)
                        || (es.OnSaleDate <= now && now < es.OffSaleDate)
                    )
                )
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

        public async Task<FilteredEventsResponse<PerformerDTO, ScheduleItemDTO>> GetFilteredEventsAsync(
            int page,
            int pageSize,
            DateTimeOffset? rangeDateFrom,
            DateTimeOffset? rangeDateTo,
            string? searchTerm,
            long? performerId,
            List<long>? eventCategoryIds,
            bool? trendingEvents,
            long matchRatio)
        {
            var query = DbContext.Set<Models.EventSchedule>()
                .Include(es => es.Event)
                .Where(es => es.StartDateTime >= DateTimeOffset.UtcNow)
                .AsQueryable();

            if (performerId != null)
            {
                query = query
                    .Where(es => es.Event.PerformerId == performerId);
            }

            if (eventCategoryIds != null && eventCategoryIds.Any())
            {
                query = query
                    .Where(es => es.Event.Categories
                        .Any(c => eventCategoryIds.Contains(c.Id))
                    );
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var text = searchTerm.ToLower();

                query = query
                    .Where(es =>
                        es.Event.Name.ToLower().Contains(text)
                        || es.Event.VenueMap.Venue.Name.ToLower().Contains(text)
                    );
            }

            if (rangeDateFrom != null)
            {
                DateTime from = rangeDateFrom.Value.Date;
                query = query
                    .Where(es => es.StartDateTime.Date >= from);
            }

            if (rangeDateTo != null)
            {
                DateTime to = rangeDateTo.Value.Date.AddDays(1);
                query = query
                    .Where(es => es.StartDateTime.Date < to);
            }

            if (trendingEvents != null && trendingEvents == true)
            {
                query = query
                    .Where(es => es.Event.ViewCount > 0)
                    .OrderByDescending(es => es.Event.ViewCount);
            }

            int totalCount = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            var rawEvents = await query
            .Skip(skip)
            .Take(pageSize)
            .Select(es => new
            {
                es.Id,
                es.StartDateTime,
                es.EventId,
                EventName = es.Event.Name,
                Location = es.Event.VenueMap.Venue.Name,
                Categories = es.Event.Categories
                    .Select(ec => new EventCategoryDTO
                    {
                        Id = ec.Id,
                        Name = ec.Name,
                        DisplayName = ec.DisplayName
                    })
                    .ToList(),
                BannerFile = es.Event.EventImages.Where(i => i.ImageType == Commons.Enums.ImageType.HorizontalPoster).OrderBy(i => i.Order).FirstOrDefault(),
                PosterFile = es.Event.EventImages.Where(i => i.ImageType == Commons.Enums.ImageType.VerticalPoster).OrderBy(i => i.Order).FirstOrDefault(),
                LegacyBannerUrl = es.Event.BannerImageUrl,
                LegacyPosterUrl = es.Event.PosterImageUrl
            })
            .ToListAsync();

            List<ScheduleItemDTO> events = rawEvents.Select(es => new ScheduleItemDTO
            {
                Id = es.Id,
                StartDate = es.StartDateTime,
                Event = new EventItemDTO
                {
                    Id = es.EventId,
                    BannerImageUrl = es.BannerFile != null
                        ? $"data:{es.BannerFile.ContentType};base64,{Convert.ToBase64String(es.BannerFile.Content)}"
                        : es.LegacyBannerUrl ?? string.Empty,
                    PosterImageUrl = es.PosterFile != null
                        ? $"data:{es.PosterFile.ContentType};base64,{Convert.ToBase64String(es.PosterFile.Content)}"
                        : es.LegacyPosterUrl ?? string.Empty,
                    Name = es.EventName,
                    StartDate = es.StartDateTime,
                    Location = es.Location,
                    Categories = es.Categories
                }
            }).ToList();

            var performerQuery = DbContext.Set<Performer>().AsQueryable();

            List<Performer> performers = new List<Performer>();
            List<PerformerDTO> performersDto = new List<PerformerDTO>();

            if (performerId != null)
            {
                performersDto = await performerQuery
                    .Where(p => p.Id == performerId)
                    .Select(p => new PerformerDTO
                    {
                        Id = p.Id,
                        Name = p.Name,
                        ImageUrl = p.ImageUrl,
                    })
                    .ToListAsync();
            }
            else if (!string.IsNullOrEmpty(searchTerm))
            {
                string filter = searchTerm;

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

            return new FilteredEventsResponse<PerformerDTO, ScheduleItemDTO>
            {
                Performers = performersDto,
                PagedEvents = new PagedResponse<ScheduleItemDTO>
                {
                    Items = events,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            };
        }
    }
}
