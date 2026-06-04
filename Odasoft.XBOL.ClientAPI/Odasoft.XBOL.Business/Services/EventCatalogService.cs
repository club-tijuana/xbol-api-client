using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Requests;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.Data;
using Odasoft.XBOL.Data.Mapping;
using Odasoft.XBOL.Data.Queries;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;
using ModelEvent = Odasoft.XBOL.Models.Event;
using ModelEventSchedule = Odasoft.XBOL.Models.EventSchedule;

namespace Odasoft.XBOL.Business.Services
{
    public class EventCatalogService(XBOLDbContext dbContext)
    {
        public async Task<PagedResponse<EventCatalogItemDTO>> GetItemsAsync(EventCatalogQueryParams queryParams)
        {
            var events = await LoadEventsAsync();
            var bundles = await LoadBundlesAsync();
            var eventMedia = await LoadCatalogMediaAsync(events.Select(eventItem => eventItem.Id), ClientSaleType.Event);
            var bundleMedia = await LoadCatalogMediaAsync(bundles.Select(bundle => bundle.Id), ClientSaleType.Bundle);

            var items = events
                .Select(eventItem => MapEvent(eventItem, eventMedia))
                .Concat(bundles.Select(bundle => MapBundle(bundle, bundleMedia)))
                .Where(item => MatchesCatalogFilters(item, queryParams))
                .ToList();

            return Page(Sort(items, queryParams.SortBy, queryParams.Descending), queryParams.Page, queryParams.PageSize);
        }

        public async Task<EventCatalogItemDTO?> GetItemAsync(long id, EventCatalogItemType? itemType = null)
        {
            if (itemType is null or EventCatalogItemType.Event)
            {
                var eventItem = (await LoadEventsAsync()).FirstOrDefault(item => item.Id == id);
                if (eventItem is not null)
                {
                    var media = await LoadCatalogMediaAsync([eventItem.Id], ClientSaleType.Event);
                    return MapEvent(eventItem, media);
                }
            }

            if (itemType is null or EventCatalogItemType.Bundle)
            {
                var bundle = (await LoadBundlesAsync()).FirstOrDefault(item => item.Id == id);
                if (bundle is not null)
                {
                    var media = await LoadCatalogMediaAsync([bundle.Id], ClientSaleType.Bundle);
                    return MapBundle(bundle, media);
                }
            }

            return null;
        }

        private async Task<List<ModelEvent>> LoadEventsAsync()
        {
            return await dbContext.Events
                .AsNoTracking()
                .AsSplitQuery()
                .Include(eventItem => eventItem.VenueMap)
                    .ThenInclude(venueMap => venueMap.Venue)
                .Include(eventItem => eventItem.Categories)
                .Include(eventItem => eventItem.Schedules)
                    .ThenInclude(schedule => schedule.Sections)
                .ToListAsync();
        }

        private async Task<List<Bundle>> LoadBundlesAsync()
        {
            return await dbContext.Bundles
                .AsNoTracking()
                .AsSplitQuery()
                .Include(bundle => bundle.VenueMap)
                    .ThenInclude(venueMap => venueMap!.Venue)
                .Include(bundle => bundle.BundleSections)
                .Include(bundle => bundle.BundleEventSchedules)
                    .ThenInclude(link => link.EventSchedule)
                    .ThenInclude(schedule => schedule.Event)
                    .ThenInclude(eventItem => eventItem.VenueMap)
                .Include(bundle => bundle.BundleEventSchedules)
                    .ThenInclude(link => link.EventSchedule)
                    .ThenInclude(schedule => schedule.Sections)
                .ToListAsync();
        }

        private async Task<Dictionary<long, EventMediaSetResponse>> LoadCatalogMediaAsync(
            IEnumerable<long> referenceIds,
            ClientSaleType referenceType)
        {
            var ids = referenceIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return [];
            }

            ClientMediaType[] mediaTypes = referenceType == ClientSaleType.Bundle
                ? [ClientMediaType.Banner, ClientMediaType.Logo]
                : [ClientMediaType.Banner, ClientMediaType.Gallery, ClientMediaType.Sponsor];

            var media = await dbContext.Media
                .AsNoTracking()
                .AvailableBlobMedia()
                .Where(item => item.ReferenceType == referenceType
                    && ids.Contains(item.ReferenceId)
                    && mediaTypes.Contains(item.MediaType))
                .OrderBy(item => item.ReferenceId)
                .ThenBy(item => item.MediaType)
                .ThenBy(item => item.Order)
                .ThenBy(item => item.Id)
                .Select(item => new
                {
                    item.ReferenceId,
                    Media = new MediaResponse
                    {
                        Id = item.Id,
                        Url = item.BlobAsset.Url,
                        ContentType = item.BlobAsset.ContentType,
                        FileName = item.BlobAsset.FileName,
                        MediaType = item.MediaType,
                        Order = item.Order
                    }
                })
                .ToListAsync();

            return media
                .GroupBy(item => item.ReferenceId)
                .ToDictionary(
                    referenceGroup => referenceGroup.Key,
                    referenceGroup => EventMediaSetMapper.CreateMediaSet(referenceGroup.Select(item => item.Media)));
        }

        private static EventCatalogItemDTO MapEvent(
            ModelEvent eventItem,
            IReadOnlyDictionary<long, EventMediaSetResponse> media)
        {
            var schedule = PickDisplaySchedule(eventItem.Schedules);
            var mediaSet = MediaSet(media, eventItem.Id);
            var bannerImageUrl = mediaSet.Banner?.Url;

            return new EventCatalogItemDTO
            {
                Id = eventItem.Id,
                ItemType = EventCatalogItemType.Event,
                Status = eventItem.Status,
                ScheduledStartDate = schedule?.StartDateTime ?? eventItem.CreatedAt,
                Name = eventItem.Name,
                Categories = Categories(eventItem.Categories),
                VenueMapId = eventItem.VenueMapId,
                VenueName = eventItem.VenueMap?.Venue?.Name,
                ExternalEventKey = schedule?.ExternalEventKey,
                AvailableSeats = schedule?.Sections.Sum(section => section.AvailableSeats) ?? 0,
                TotalSeats = schedule?.Sections.Sum(section => section.TotalSeats) ?? 0,
                Media = mediaSet,
                PosterImageUrl = bannerImageUrl ?? eventItem.PosterImageUrl,
                BannerImageUrl = bannerImageUrl ?? eventItem.BannerImageUrl
            };
        }

        private static EventCatalogItemDTO MapBundle(
            Bundle bundle,
            IReadOnlyDictionary<long, EventMediaSetResponse> media)
        {
            var schedules = BundleSchedules(bundle).ToList();
            var schedule = PickDisplaySchedule(schedules);
            var venueMapId = bundle.VenueMapId ?? schedule?.Event?.VenueMapId;
            var mediaSet = MediaSet(media, bundle.Id);
            var bannerImageUrl = mediaSet.Banner?.Url;
            var posterImageUrl = mediaSet.Logo?.Url;

            return new EventCatalogItemDTO
            {
                Id = bundle.Id,
                ItemType = EventCatalogItemType.Bundle,
                BundleType = bundle.BundleType,
                Status = bundle.Status,
                ScheduledStartDate = schedule?.StartDateTime ?? bundle.StartDate ?? bundle.CreatedAt,
                Name = bundle.Name,
                Code = bundle.Code,
                VenueMapId = bundle.VenueMapId,
                VenueName = bundle.VenueMap?.Venue?.Name ?? schedule?.Event?.VenueMap?.Venue?.Name,
                ExternalEventKey = bundle.ExternalKey,
                AvailableSeats = bundle.BundleSections.Sum(section => section.AvailableSeats),
                TotalSeats = bundle.BundleSections.Sum(section => section.TotalSeats),
                Media = mediaSet,
                PosterImageUrl = posterImageUrl ?? bundle.PosterImageUrl,
                BannerImageUrl = bannerImageUrl ?? bundle.BannerImageUrl,
                IsSeason = bundle.BundleType == BundleType.SeasonPass
            };
        }

        private static EventMediaSetResponse MediaSet(
            IReadOnlyDictionary<long, EventMediaSetResponse> media,
            long referenceId)
        {
            return media.GetValueOrDefault(referenceId) ?? new EventMediaSetResponse();
        }

        private static IEnumerable<ModelEventSchedule> BundleSchedules(Bundle bundle)
        {
            return bundle.BundleEventSchedules
                .Select(link => link.EventSchedule)
                .Where(schedule => schedule is not null);
        }

        private static ModelEventSchedule? PickDisplaySchedule(IEnumerable<ModelEventSchedule> schedules)
        {
            var ordered = schedules.OrderBy(schedule => schedule.StartDateTime).ToList();
            return ordered.FirstOrDefault(schedule => schedule.StartDateTime >= DateTimeOffset.UtcNow) ??
                   ordered.LastOrDefault();
        }

        private static List<EventCategoryDTO> Categories(IEnumerable<Odasoft.XBOL.Models.EventCategory> categories)
        {
            return categories.Select(category => new EventCategoryDTO
            {
                Id = category.Id,
                Name = category.Name,
                DisplayName = category.DisplayName
            }).ToList();
        }

        private static bool MatchesCatalogFilters(EventCatalogItemDTO item, EventCatalogQueryParams queryParams)
        {
            if (!MatchesSearch(item.Name, queryParams.SearchTerm))
            {
                return false;
            }

            if (queryParams.ItemType is not null && item.ItemType != queryParams.ItemType)
            {
                return false;
            }

            if (queryParams.BundleType is not null && item.BundleType != queryParams.BundleType)
            {
                return false;
            }

            if (queryParams.Status is not null && item.Status != queryParams.Status)
            {
                return false;
            }

            if (!MatchesVenue(item.VenueName, queryParams.Venue))
            {
                return false;
            }

            if (!MatchesDateRange(item.ScheduledStartDate, queryParams.StartDate, queryParams.EndDate))
            {
                return false;
            }

            if (queryParams.Upcoming is null)
            {
                return true;
            }

            return queryParams.Upcoming.Value
                ? item.ScheduledStartDate >= DateTimeOffset.UtcNow
                : item.ScheduledStartDate < DateTimeOffset.UtcNow;
        }

        private static bool MatchesSearch(string value, string? searchTerm)
        {
            return string.IsNullOrWhiteSpace(searchTerm) ||
                   value.Contains(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesVenue(string? venueName, string? venueFilter)
        {
            return string.IsNullOrWhiteSpace(venueFilter) ||
                   (!string.IsNullOrWhiteSpace(venueName) &&
                    venueName.Contains(venueFilter.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static bool MatchesDateRange(
            DateTimeOffset value,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate)
        {
            if (startDate is not null && value < startDate.Value)
            {
                return false;
            }

            if (endDate is not null && value > endDate.Value)
            {
                return false;
            }

            return true;
        }

        private static List<EventCatalogItemDTO> Sort(
            IEnumerable<EventCatalogItemDTO> items,
            string? sortBy,
            bool descending)
        {
            return (NormalizeSort(sortBy), descending) switch
            {
                ("name", true) => [.. items.OrderByDescending(item => item.Name).ThenBy(item => item.Id)],
                ("name", false) => [.. items.OrderBy(item => item.Name).ThenBy(item => item.Id)],
                ("status", true) => [.. items.OrderByDescending(item => item.Status).ThenBy(item => item.Id)],
                ("status", false) => [.. items.OrderBy(item => item.Status).ThenBy(item => item.Id)],
                (_, true) => [.. items.OrderByDescending(item => item.ScheduledStartDate)
                    .ThenBy(item => item.ItemType)
                    .ThenBy(item => item.Id)],
                (_, false) => [.. items.OrderBy(item => item.ScheduledStartDate)
                    .ThenBy(item => item.ItemType)
                    .ThenBy(item => item.Id)]
            };
        }

        private static string NormalizeSort(string? sortBy)
        {
            return string.IsNullOrWhiteSpace(sortBy)
                ? "startdate"
                : sortBy.Trim().Replace(" ", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
        }

        private static PagedResponse<EventCatalogItemDTO> Page(
            IReadOnlyCollection<EventCatalogItemDTO> items,
            int page,
            int pageSize)
        {
            var normalizedPage = Math.Max(page, 1);
            var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

            return new PagedResponse<EventCatalogItemDTO>
            {
                Items = [.. items.Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize)],
                TotalCount = items.Count,
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                TotalPages = (int)Math.Ceiling(items.Count / (double)normalizedPageSize)
            };
        }
    }
}
