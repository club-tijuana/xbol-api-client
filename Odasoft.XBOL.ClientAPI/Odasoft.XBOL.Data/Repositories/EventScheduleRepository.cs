using FuzzySharp;
using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.Data.Mapping;
using Odasoft.XBOL.Data.Queries;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class EventScheduleRepository(XBOLDbContext dbContext) : BaseRepository<EventSchedule>(dbContext)
    {
        public async Task<FilteredEventsResponse<PerformerDTO, ScheduleItemDTO>> GetFilteredEventsAsync(
            int page,
            int pageSize,
            DateTimeOffset? rangeDateFrom,
            DateTimeOffset? rangeDateTo,
            string? searchTerm,
            long? performerId,
            List<long>? eventCategoryIds,
            bool? trendingEvents,
            long matchRatio,
            bool includeMedia = false)
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
            .GroupJoin(
                DbContext.Set<Media>().AvailableBlobMedia().Where(x => x.ReferenceType == ClientSaleType.Event),
                eventObject => eventObject.EventId,
                media => media.ReferenceId,
                (es, m) => new
                {
                    EventSchedule = es,
                    EventImages = m
                }
            )
            .Skip(skip)
            .Take(pageSize)
            .Select(es => new
            {
                es.EventSchedule.Id,
                es.EventSchedule.StartDateTime,
                es.EventSchedule.EventId,
                EventName = es.EventSchedule.Event.Name,
                Location = es.EventSchedule.Event.VenueMap.Venue.Name,
                Categories = es.EventSchedule.Event.Categories
                    .Select(ec => new EventCategoryDTO
                    {
                        Id = ec.Id,
                        Name = ec.Name,
                        DisplayName = ec.DisplayName
                    })
                    .ToList(),
                BannerUrl = es.EventImages.Where(i => i.MediaType == ClientMediaType.Banner).OrderBy(i => i.Order).Select(i => i.BlobAsset.Url).FirstOrDefault(),
                LegacyBannerUrl = es.EventSchedule.Event.BannerImageUrl,
                LegacyPosterUrl = es.EventSchedule.Event.PosterImageUrl
            })
            .ToListAsync();

            List<ScheduleItemDTO> events = rawEvents.Select(es => new ScheduleItemDTO
            {
                Id = es.Id,
                StartDate = es.StartDateTime,
                Event = new EventItemDTO
                {
                    Id = es.EventId,
                    BannerImageUrl = es.BannerUrl != null
                        ? es.BannerUrl
                        : es.LegacyBannerUrl ?? string.Empty,
                    PosterImageUrl = es.BannerUrl != null
                        ? es.BannerUrl
                        : es.LegacyPosterUrl ?? string.Empty,
                    Name = es.EventName,
                    StartDate = es.StartDateTime,
                    Location = es.Location,
                    Categories = es.Categories
                }
            }).ToList();

            if (includeMedia && events.Count > 0)
            {
                var eventIds = events.Select(x => x.Event.Id).Distinct().ToList();
                var mediaSets = await EventMediaSetMapper.GetEventMediaSetsAsync(DbContext, eventIds);

                foreach (var schedule in events)
                {
                    schedule.Event.Media = mediaSets.GetValueOrDefault(schedule.Event.Id) ?? new EventMediaSetResponse();
                }
            }

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
