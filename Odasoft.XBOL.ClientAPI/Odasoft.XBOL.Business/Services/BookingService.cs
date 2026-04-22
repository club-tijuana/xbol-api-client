using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Requests.Filters;
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
        private readonly SeasonRepository _seasonRepository;
        private readonly SeasonPassRepository _seasonPassRepository;
        private readonly OrderRepository _orderRepository;
        private readonly ClientService _clientService;

        public BookingService(EventSectionRepository eventSectionRepository,
            EventScheduleRepository eventScheduleRepository,
            SeasonRepository seasonRepository,
            SeasonPassRepository seasonPassRepository,
            OrderRepository orderRepository,
            ClientService clientService)
        {
            _eventSectionRepository = eventSectionRepository;
            _eventScheduleRepository = eventScheduleRepository;
            _seasonRepository = seasonRepository;
            _orderRepository = orderRepository;
            _clientService = clientService;
            _seasonPassRepository = seasonPassRepository;
        }

        public async Task<IList<ZoneDTO>> GetZonesByEventIdAsync(long scheduleId)
        {
            return await _eventSectionRepository.GetZonesByEventIdAsync(scheduleId);
        }

        public async Task<IList<ZoneDTO>> GetZonesBySeasonIdAsync(long seasonId)
        {
            return await _eventSectionRepository.GetZonesBySeasonIdAsync(seasonId);
        }

        public async Task<SeatAvailabilityDTO> GetSeatAvailabilityAsync(ReservationFilters filters)
        {
            return await _eventSectionRepository.GetSeatAvailabilityAsync(filters);
        }

        public async Task<EventItemDTO> GetEventItemByScheduleIdAsync(long scheduleId)
        {
            //return await _eventScheduleRepository.GetEventItemByScheduleIdAsync(scheduleId);
            var schedule = await _eventScheduleRepository.Get(
                s => s.Id == scheduleId)
                .Include(s => s.Event)
                    .ThenInclude(e => e.VenueMap)
                .Include(s => s.Event)
                    .ThenInclude(e => e.Categories)
                .Include(s => s.Event)
                    .ThenInclude(e => e.EventImages)
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

            var banner = schedule.Event.EventImages
                .Where(i => i.ImageType == Commons.Enums.ImageType.HorizontalPoster)
                .OrderBy(i => i.Order)
                .FirstOrDefault();

            var poster = schedule.Event.EventImages
                .Where(i => i.ImageType == Commons.Enums.ImageType.VerticalPoster)
                .OrderBy(i => i.Order)
                .FirstOrDefault();

            return new EventItemDTO
            {
                Id = schedule.Id,
                BannerImageUrl = banner != null
                    ? $"data:{banner.ContentType};base64,{Convert.ToBase64String(banner.Content)}"
                    : schedule.Event.BannerImageUrl ?? string.Empty,
                PosterImageUrl = poster != null
                    ? $"data:{poster.ContentType};base64,{Convert.ToBase64String(poster.Content)}"
                    : schedule.Event.PosterImageUrl ?? string.Empty,
                Name = schedule.Event.Name,
                StartDate = schedule.StartDateTime,
                Location = schedule.Event.VenueMap.Name,
                Categories = schedule.Event.Categories
                        .Select(ec => new EventCategoryDTO
                        {
                            Id = ec.Id,
                            Name = ec.Name,
                            DisplayName = ec.DisplayName
                        }).ToList(),
                EventKey = schedule.ExternalEventKey
            };
        }

        public async Task<SeasonItemDTO?> GetSeasonByIdAsync(long seasonId, long? clientId)
        {
            var now = DateTimeOffset.UtcNow;

            var season = await _seasonRepository.Get(s => s.Id == seasonId)
                .FirstOrDefaultAsync();

            if (season == null)
            {
                throw new Exception("Season not found");
            }

            var result = await CanReserveSeasonAsync(season, clientId);

            if (!result.CanReserve)
            {
                throw new Exception(result.Message);
            }

            return Map(season);
        }

        public async Task<ReservationAvailabilityResult> CanReserveSeasonAsync(Season season, long? clientId)
        {
            var now = DateTimeOffset.UtcNow;

            var isRenewal = now >= season.RenewalStartDate && now <= season.RenewalEndDate;
            var isPreSale = now >= season.PreSaleDate && now < season.OnSaleDate;
            var isGeneral = now >= season.OnSaleDate && now < season.OffSaleDate;

            var hasStarted = now >= season.RenewalStartDate;
            var isExpired = now >= season.OffSaleDate;

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

            if (season.PreviousSeasonId.HasValue)
            {
                var seasonPassIds = await _seasonPassRepository.Get(sp =>
                    sp.ClientId == clientId
                    && sp.SeasonId == season.PreviousSeasonId
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
                Message = "The season is not available at this time"
            };
        }

        public async Task<ReservationAvailabilityResult> CanReserveEventAsync(EventSchedule eventSchedule)
        {
            var now = DateTimeOffset.UtcNow;

            var isPreSale = now >= eventSchedule.PreSaleStartDate && now <= eventSchedule.PreSaleEndDate;
            var isGeneral = now >= eventSchedule.OnSaleDate && now < eventSchedule.OffSaleDate;

            var hasStarted = now >= eventSchedule.PreSaleStartDate;
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

        private static SeasonItemDTO Map(Season season)
        {
            return new SeasonItemDTO
            {
                Id = season.Id,
                BannerImageUrl = season.BannerImageUrl,
                StartDate = season.StartDate,
                ExternalSeasonKey = season.ExternalSeasonKey
            };
        }
    }
}
