using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Requests.Filters;
using Odasoft.XBOL.Data.Mapping;
using Odasoft.XBOL.Data.Queries;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.DTO.Results;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Business.Services
{
    public class BookingService
    {
        private readonly EventSectionRepository _eventSectionRepository;
        private readonly EventScheduleRepository _eventScheduleRepository;
        private readonly MediaRepository _mediaRepository;
        private readonly SeasonRepository _seasonRepository;
        private readonly SeasonPassRepository _seasonPassRepository;
        private readonly OrderRepository _orderRepository;
        private readonly ClientService _clientService;
        private readonly BundleRepository _bundleRepository;
        private readonly BundlePassRepository _bundlePassRepository;
        private readonly ITicketingClient _ticketingClient;

        public BookingService(EventSectionRepository eventSectionRepository,
            EventScheduleRepository eventScheduleRepository,
            MediaRepository mediaRepository,
            SeasonRepository seasonRepository,
            SeasonPassRepository seasonPassRepository,
            OrderRepository orderRepository,
            ClientService clientService,
            BundleRepository bundleRepository,
            BundlePassRepository bundlePassRepository,
            ITicketingClient ticketingClient)
        {
            _eventSectionRepository = eventSectionRepository;
            _eventScheduleRepository = eventScheduleRepository;
            _mediaRepository = mediaRepository;
            _seasonRepository = seasonRepository;
            _orderRepository = orderRepository;
            _clientService = clientService;
            _seasonPassRepository = seasonPassRepository;
            _ticketingClient = ticketingClient;
            _bundleRepository = bundleRepository;
            _bundlePassRepository = bundlePassRepository;
        }

        public async Task<IList<ZoneDTO>> GetZonesByEventIdAsync(long scheduleId)
        {
            return await _eventSectionRepository.GetZonesByEventIdAsync(scheduleId);
        }

        public async Task<IList<ZoneDTO>> GetZonesBySeasonIdAsync(long seasonId)
        {
            return await _eventSectionRepository.GetZonesBySeasonIdAsync(seasonId);
        }

        public async Task<IList<ZoneDTO>> GetZonesByBundleIdAsync(long bundleId)
        {
            return await _eventSectionRepository.GetZonesByBundleIdAsync(bundleId);
        }

        public async Task<SeatAvailabilityDTO> GetSeatAvailabilityAsync(ReservationFilters filters)
        {
            SeatAvailabilityResponse response = await _ticketingClient.GetSeatAvailabilityAsync(
                filters.SeasonId,
                filters.ScheduleId,
                filters.SectionId,
                filters.ZoneId,
                filters.PriceRange?.Min,
                filters.PriceRange?.Max);

            return new SeatAvailabilityDTO
            {
                Zones = response.Zones?.Select(x => new ZoneDTO
                {
                    Id = x.Id.HasValue ? x.Id.Value : 0,
                    Name = x.Name ?? string.Empty,
                    DisplayName = x.DisplayName ?? string.Empty,
                    Price = x.Price,
                    PriceListItemId = x.PriceListItemId
                }).ToList() ?? [],
                SeatOverrides = response.SeatOverrides?.Select(x => new SeatDTO
                {
                    Id = x.Id.HasValue ? x.Id.Value : 0,
                    ExternalSeatObjectKey = x.ExternalSeatObjectKey ?? string.Empty,
                    PriceOverride = x.PriceOverride,
                    PriceListItemId = x.PriceListItemId
                }).ToList() ?? []
            };
        }

        public async Task<EventItemDTO> GetEventItemByScheduleIdAsync(long scheduleId, bool includeMedia = false)
        {
            var schedule = await _eventScheduleRepository.Get(
                s => s.Id == scheduleId)
                .Include(s => s.Event)
                    .ThenInclude(e => e.VenueMap)
                    .ThenInclude(e => e.Venue)
                .Include(s => s.Event)
                    .ThenInclude(e => e.Categories)
                .Include(s => s.Event)
                .FirstOrDefaultAsync();

            if (schedule == null)
            {
                throw new Exception("Event schedule not found");
            }

            var canReserve = await CanReserveEventAsync(schedule);
            if (!canReserve.CanReserve)
            {
                throw new Exception(canReserve.Message);
            }

            var eventMedia = await _mediaRepository
                .Get(filter: m =>
                    m.ReferenceId == schedule.EventId &&
                    m.ReferenceType == ClientSaleType.Event,
                    includedProperties: "BlobAsset"
                )
                .AvailableBlobMedia()
                .ToListAsync();

            var banner = eventMedia
                .Where(m => m.MediaType == ClientMediaType.Banner)
                .OrderBy(m => m.Order)
                .FirstOrDefault();

            return new EventItemDTO
            {
                Id = schedule.Id,
                BannerImageUrl = banner != null && banner.Url != null
                    ? banner.Url
                    : schedule.Event.BannerImageUrl ?? string.Empty,
                PosterImageUrl = banner != null && banner.Url != null
                    ? banner.Url
                    : schedule.Event.PosterImageUrl ?? string.Empty,
                Name = schedule.Event.Name,
                StartDate = schedule.StartDateTime,
                Location = schedule.Event.VenueMap.Venue.Name,
                Categories = schedule.Event.Categories
                        .Select(ec => new EventCategoryDTO
                        {
                            Id = ec.Id,
                            Name = ec.Name,
                            DisplayName = ec.DisplayName
                        }).ToList(),
                EventKey = schedule.ExternalEventKey,
                Media = includeMedia
                    ? EventMediaSetMapper.CreateMediaSet(eventMedia.Select(EventMediaSetMapper.CreateMediaResponse))
                    : null
            };
        }

        //public async Task<SeasonItemDTO?> GetSeasonByIdAsync(long seasonId, long? clientId, bool includeMedia = false)
        //{
        //    var now = DateTimeOffset.UtcNow;

        //    var season = await _seasonRepository.Get(s => s.Id == seasonId)
        //        .FirstOrDefaultAsync();

        //    if (season == null)
        //    {
        //        throw new Exception("Season not found");
        //    }

        //    var result = await CanReserveSeasonAsync(season, clientId);

        //    if (!result.CanReserve)
        //    {
        //        throw new Exception(result.Message);
        //    }

        //    return await MapSeasonItemAsync(season, includeMedia);
        //}
        public async Task<BundleItemDTO?> GetBundleByIdAsync(long bundleId, long? clientId, bool includeMedia = false)
        {
            var now = DateTimeOffset.UtcNow;

            var bundle = await _bundleRepository.Get(b => b.Id == bundleId)
                .FirstOrDefaultAsync();

            if (bundle == null)
            {
                throw new Exception("Bundle not found");
            }

            var result = await CanReserveBundleAsync(bundle, clientId);

            if (!result.CanReserve)
            {
                throw new Exception(result.Message);
            }

            return await MapBundleSeasonItemAsync(bundle, includeMedia);
        }

        public async Task<BundleItemDTO?> GetBundleSeasonByIdAsync(long bundleId, long? clientId, bool includeMedia = false)
        {
            var now = DateTimeOffset.UtcNow;

            var bundle = await _bundleRepository.Get(
                s => s.Id == bundleId,
                includedProperties:
                [
                    "BundleEventSchedules.EventSchedule.Event.VenueMap.Venue"
                ])
                .FirstOrDefaultAsync();

            if (bundle == null)
            {
                throw new Exception("Bundle not found");
            }

            var result = await CanReserveBundleSeasonAsync(bundle, clientId);

            if (!result.CanReserve)
            {
                throw new Exception(result.Message);
            }

            return await MapBundleSeasonItemAsync(bundle, includeMedia);
        }

        //public async Task<ReservationAvailabilityResult> CanReserveSeasonAsync(Season season, long? clientId)
        //{
        //    var now = DateTimeOffset.UtcNow;

        //    var isRenewal = now >= season.RenewalStartDate && now <= season.RenewalEndDate;
        //    var isPreSale = now >= season.PreSaleDate && now < season.OnSaleDate;
        //    var isGeneral = now >= season.OnSaleDate && now < season.OffSaleDate;

        //    var hasStarted = season.RenewalStartDate == null ? true : now >= season.RenewalStartDate;
        //    var isExpired = now >= season.OffSaleDate;

        //    if (isExpired)
        //    {
        //        return new ReservationAvailabilityResult
        //        {
        //            CanReserve = false,
        //            Message = "The season is no longer available"
        //        };
        //    }

        //    if (!hasStarted)
        //    {
        //        return new ReservationAvailabilityResult
        //        {
        //            CanReserve = false,
        //            Message = "The season is not yet available"
        //        };
        //    }

        //    if (clientId == null)
        //    {
        //        if (!isGeneral)
        //        {
        //            return new ReservationAvailabilityResult
        //            {
        //                CanReserve = false,
        //                Message = "General sale has not started yet"
        //            };
        //        }

        //        return new ReservationAvailabilityResult { CanReserve = true };
        //    }

        //    bool hasPreviousSeason = false;

        //    if (season.PreviousSeasonId.HasValue)
        //    {
        //        var seasonPassIds = await _seasonPassRepository.Get(sp =>
        //            sp.ClientId == clientId
        //            && sp.SeasonId == season.PreviousSeasonId
        //        )
        //        .Select(sp => sp.Id)
        //        .ToListAsync();

        //        hasPreviousSeason = await _orderRepository.Get(o =>
        //            o.ClientId == clientId
        //            && o.OrderType == Commons.Enums.OrderType.SeasonPass
        //            && o.Items.Any(i => seasonPassIds.Contains(i.ItemReferenceId))
        //        ).AnyAsync();
        //    }

        //    if (!hasPreviousSeason)
        //    {
        //        if (!isGeneral)
        //        {
        //            return new ReservationAvailabilityResult
        //            {
        //                CanReserve = false,
        //                Message = "Available only during general sale"
        //            };
        //        }

        //        return new ReservationAvailabilityResult { CanReserve = true };
        //    }

        //    if (isRenewal || isPreSale || isGeneral)
        //    {
        //        return new ReservationAvailabilityResult { CanReserve = true };
        //    }

        //    return new ReservationAvailabilityResult
        //    {
        //        CanReserve = false,
        //        Message = "The season is not available at this time"
        //    };
        //}

        public async Task<ReservationAvailabilityResult> CanReserveBundleAsync(Bundle bundle, long? clientId)
        {
            var now = DateTimeOffset.UtcNow;

            var isRenewal = now >= bundle.RenewalStartDate && now <= bundle.RenewalEndDate;
            var isPreSale = now >= bundle.PreSaleDate && now < bundle.OnSaleDate;
            var isGeneral = now >= bundle.OnSaleDate && now < bundle.OffSaleDate;

            var hasStarted = bundle.RenewalStartDate == null ? true : now >= bundle.RenewalStartDate;
            var isExpired = now >= bundle.OffSaleDate;

            if (isExpired)
            {
                return new ReservationAvailabilityResult
                {
                    CanReserve = false,
                    Message = "The season is no longer available"
                };
            }

            if (!hasStarted)
            {
                return new ReservationAvailabilityResult
                {
                    CanReserve = false,
                    Message = "The season is not yet available"
                };
            }

            if (clientId == null)
            {
                if (!isGeneral)
                {
                    return new ReservationAvailabilityResult
                    {
                        CanReserve = false,
                        Message = "General sale has not started yet"
                    };
                }

                return new ReservationAvailabilityResult { CanReserve = true };
            }

            bool hasPreviousSeason = false;

            if (bundle.PreviousBundleId.HasValue)
            {
                var seasonPassIds = await _seasonPassRepository.Get(sp =>
                    sp.ClientId == clientId
                    && sp.SeasonId == bundle.PreviousBundleId
                )
                .Select(sp => sp.Id)
                .ToListAsync();

                hasPreviousSeason = await _orderRepository.Get(o =>
                    o.ClientId == clientId
                    && o.OrderType == Commons.Enums.OrderType.SeasonPass
                    && o.Items.Any(i => seasonPassIds.Contains(i.ItemReferenceId))
                ).AnyAsync();
            }

            if (!hasPreviousSeason)
            {
                if (!isGeneral)
                {
                    return new ReservationAvailabilityResult
                    {
                        CanReserve = false,
                        Message = "Available only during general sale"
                    };
                }

                return new ReservationAvailabilityResult { CanReserve = true };
            }

            if (isRenewal || isPreSale || isGeneral)
            {
                return new ReservationAvailabilityResult { CanReserve = true };
            }

            return new ReservationAvailabilityResult
            {
                CanReserve = false,
                Message = "The bundle is not available at this time"
            };
        }

        public async Task<ReservationAvailabilityResult> CanReserveBundleSeasonAsync(Bundle bundle, long? clientId)
        {
            var now = DateTimeOffset.UtcNow;

            var isRenewal = now >= bundle.RenewalStartDate && now <= bundle.RenewalEndDate;
            var isPreSale = now >= bundle.PreSaleDate && now < bundle.OnSaleDate;
            var isGeneral = now >= bundle.OnSaleDate && now < bundle.OffSaleDate;

            var hasStarted = bundle.RenewalStartDate == null ? true : now >= bundle.RenewalStartDate;
            var isExpired = now >= bundle.OffSaleDate;

            if (isExpired)
            {
                return new ReservationAvailabilityResult
                {
                    CanReserve = false,
                    Message = "The bundle is no longer available"
                };
            }

            if (!hasStarted)
            {
                return new ReservationAvailabilityResult
                {
                    CanReserve = false,
                    Message = "The bundle is not yet available"
                };
            }

            if (clientId == null)
            {
                if (!isGeneral)
                {
                    return new ReservationAvailabilityResult
                    {
                        CanReserve = false,
                        Message = "General sale has not started yet"
                    };
                }

                return new ReservationAvailabilityResult { CanReserve = true };
            }

            bool hasPreviousBundle = false;

            if (bundle.PreviousBundleId.HasValue)
            {
                var bundlePassIds = await _bundlePassRepository.Get(bp =>
                    bp.ClientId == clientId
                    && bp.BundleId == bundle.PreviousBundleId
                )
                .Select(sp => sp.Id)
                .ToListAsync();

                hasPreviousBundle = await _orderRepository.Get(o =>
                    o.ClientId == clientId
                    && o.OrderType == Commons.Enums.OrderType.Bundle
                    && o.Items.Any(i => bundlePassIds.Contains(i.ItemReferenceId))
                ).AnyAsync();
            }

            if (!hasPreviousBundle)
            {
                if (!isGeneral)
                {
                    return new ReservationAvailabilityResult
                    {
                        CanReserve = false,
                        Message = "Available only during general sale"
                    };
                }

                return new ReservationAvailabilityResult { CanReserve = true };
            }

            if (isRenewal || isPreSale || isGeneral)
            {
                return new ReservationAvailabilityResult { CanReserve = true };
            }

            return new ReservationAvailabilityResult
            {
                CanReserve = false,
                Message = "The bundle is not available at this time"
            };
        }

        public async Task<ReservationAvailabilityResult> CanReserveEventAsync(EventSchedule eventSchedule)
        {
            var now = DateTimeOffset.UtcNow;

            var isPreSale =
                eventSchedule.PreSaleStartDate is not null &&
                eventSchedule.PreSaleEndDate is not null &&
                now >= eventSchedule.PreSaleStartDate &&
                now <= eventSchedule.PreSaleEndDate;
            var isGeneral = now >= eventSchedule.OnSaleDate && now < eventSchedule.OffSaleDate;

            var hasStarted = now >= (eventSchedule.PreSaleStartDate ?? eventSchedule.OnSaleDate);
            var isExpired = now >= eventSchedule.OffSaleDate;

            if (isExpired)
            {
                return new ReservationAvailabilityResult
                {
                    CanReserve = false,
                    Message = "The event is no longer available"
                };
            }

            if (!hasStarted)
            {
                return new ReservationAvailabilityResult
                {
                    CanReserve = false,
                    Message = "The event is not yet available"
                };
            }

            if (!isGeneral)
            {
                return new ReservationAvailabilityResult
                {
                    CanReserve = false,
                    Message = "General sale has not started yet"
                };
            }

            return new ReservationAvailabilityResult { CanReserve = true };
        }

        private async Task<SeasonItemDTO> MapSeasonItemAsync(Season season, bool includeMedia)
        {
            var media = await _mediaRepository
                .Get(filter: m =>
                    m.ReferenceId == season.Id &&
                    m.ReferenceType == ClientSaleType.SeasonPass,
                    includedProperties: "BlobAsset"
                )
                .AvailableBlobMedia()
                .ToListAsync();

            var banner = media
                .Where(m => m.MediaType == ClientMediaType.Banner)
                .OrderBy(m => m.Order)
                .FirstOrDefault();

            var now = DateTimeOffset.UtcNow;

            return new SeasonItemDTO
            {
                Id = season.Id,
                BannerImageUrl = banner != null && banner.Url != null
                    ? banner.Url
                    : season.BannerImageUrl,
                StartDate = season.StartDate,
                ExternalSeasonKey = season.ExternalSeasonKey,
                Media = includeMedia
                    ? EventMediaSetMapper.CreateMediaSet(media.Select(EventMediaSetMapper.CreateMediaResponse))
                    : null,
                IsRenewal = now >= season.RenewalStartDate && now < season.RenewalEndDate,
                IsPreSale = now >= season.PreSaleDate && now < season.OnSaleDate,
                IsGeneralSale = now >= season.OnSaleDate
            };
        }

        private async Task<BundleItemDTO> MapBundleSeasonItemAsync(Bundle bundle, bool includeMedia)
        {
            var media = await _mediaRepository
                .Get(filter: m =>
                    m.ReferenceId == bundle.Id &&
                    m.ReferenceType == ClientSaleType.SeasonPass,
                    includedProperties: "BlobAsset"
                )
                .AvailableBlobMedia()
                .ToListAsync();

            var banner = media
                .Where(m => m.MediaType == ClientMediaType.Banner)
                .OrderBy(m => m.Order)
                .FirstOrDefault();

            var now = DateTimeOffset.UtcNow;

            return new BundleItemDTO
            {
                Id = bundle.Id,
                BannerImageUrl = banner != null && banner.Url != null
                    ? banner.Url
                    : bundle.BannerImageUrl,
                Name = bundle.Name,
                Location = bundle.BundleEventSchedules.First().EventSchedule.Event.VenueMap.Venue.Name,
                StartDate = bundle.StartDate,
                ExternalKey = bundle.ExternalKey,
                Media = includeMedia
                    ? EventMediaSetMapper.CreateMediaSet(media.Select(EventMediaSetMapper.CreateMediaResponse))
                    : null,
                IsRenewal = now >= bundle.RenewalStartDate && now < bundle.RenewalEndDate,
                IsPreSale = now >= bundle.PreSaleDate && now < bundle.OnSaleDate,
                IsGeneralSale = now >= bundle.OnSaleDate
            };
        }
    }
}