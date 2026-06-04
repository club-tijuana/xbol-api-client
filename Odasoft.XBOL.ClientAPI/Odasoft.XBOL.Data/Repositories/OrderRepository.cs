using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.Data.Queries;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class OrderRepository(XBOLDbContext dbContext) : BaseRepository<Order>(dbContext)
    {
        private const string SEASONPASS = "SEASONPASS";

        public async Task<PagedResponse<MyEventDTO>> GetMyEventsAsync(
            int page,
            int pageSize,
            OrderType orderType,
            long idClient)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var renewableSeasonIds = await DbContext.Set<Season>()
                .Where(s =>
                    s.PreviousSeasonId != null &&
                    s.RenewalStartDate <= now &&
                    s.RenewalEndDate >= now &&
                    !DbContext.Set<Season>().Any(s2 => s2.PreviousSeasonId == s.Id)
                )
                .Select(s => s.PreviousSeasonId!.Value)
                .ToHashSetAsync();

            var query = DbContext.Set<Models.Order>()
                .Where(o =>
                    o.Tickets.Any()
                    && o.Tickets.Any(t =>
                        t.OriginalClientId == idClient
                        || t.CurrentClientId == idClient
                    )
                    && o.OrderType == orderType
                );

            int totalCount = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            var orders = await query
                .OrderBy(o => o.Id)
                .Skip(skip)
                .Take(pageSize)
                .Select(o => new
                {
                    o.Id,
                    Tickets = o.Tickets
                        .Where(t =>
                            t.OriginalClientId == idClient ||
                            t.CurrentClientId == idClient
                        )
                        .Select(t => new
                        {
                            t.EventScheduleId,
                            t.EventSchedule.StartDateTime,
                            t.EventSchedule.EndDateTime,
                            EventId = t.EventSchedule.EventId,
                            EventName = t.EventSchedule.Event.Name,
                            LegacyPosterUrl = t.EventSchedule.Event.PosterImageUrl,
                            Location = t.EventSchedule.Event.VenueMap.Name,
                            TicketType = t.TicketType,
                            SeasonId = t.EventSchedule.Event.SeasonId,
                            SeasonName = t.EventSchedule.Event.Season != null ? t.EventSchedule.Event.Season.Name : ""
                        })
                        .ToList()
                })
                .ToListAsync();

            var orderIds = orders.Select(o => o.Id).ToHashSet();

            var renewedOrderIds = await DbContext.Set<Models.Order>()
                .Where(o =>
                    o.RelatedOrderId != null &&
                    orderIds.Contains(o.RelatedOrderId.Value)
                )
                .Select(o => o.RelatedOrderId!.Value)
                .ToHashSetAsync();

            var events = orders.Select(o =>
            {
                bool isSeason = o.Tickets.Any(t => t.TicketType.ToUpper().Trim() == SEASONPASS);

                var currentSchedule = o.Tickets
                    .GroupBy(t => t.EventScheduleId)
                    .Select(g => g.First())
                    .OrderBy(t => t.StartDateTime < now)
                    .ThenByDescending(t => t.StartDateTime)
                    .First();

                bool isPastEvent;

                if (isSeason)
                {
                    isPastEvent = o.Tickets.All(t => t.EndDateTime < now);
                }
                else
                {
                    isPastEvent = currentSchedule.StartDateTime < now;
                }

                bool alreadyRenewed = renewedOrderIds.Contains(o.Id);

                bool canRenovateSeasonPass =
                    isSeason &&
                    isPastEvent &&
                    currentSchedule.SeasonId.HasValue &&
                    renewableSeasonIds.Contains(currentSchedule.SeasonId.Value) &&
                    !alreadyRenewed;

                var banner = DbContext.Set<Media>()
                .Include(m => m.BlobAsset)
                .AvailableBlobMedia()
                .Where(m =>
                    m.ReferenceId == currentSchedule.EventId &&
                    m.ReferenceType == ClientSaleType.Event &&
                    m.MediaType == ClientMediaType.Banner
                )
                .OrderBy(m => m.Order)
                .FirstOrDefault();

                return new MyEventDTO
                {
                    OrderId = o.Id,
                    EventId = currentSchedule.EventId,
                    EventImage = banner != null && banner.Url != null
                        ? banner.Url
                        : currentSchedule.LegacyPosterUrl ?? string.Empty,
                    Name = isSeason ? currentSchedule.SeasonName : currentSchedule.EventName,
                    StartDate = currentSchedule.StartDateTime,
                    Location = currentSchedule.Location,
                    IsSeasonPass = isSeason,
                    IsPastEvent = isPastEvent,
                    CanRenovateSeasonPass = canRenovateSeasonPass
                };
            }).ToList();

            return new PagedResponse<MyEventDTO>
            {
                Items = events,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<MyEventDetailDTO?> GetMyEventDetailAsync(long clientId, long eventId, long orderId)
        {
            var result = await DbContext.Set<Order>()
                .Where(o =>
                    o.Id == orderId &&
                    o.Tickets.Any(t =>
                        t.OriginalClientId == clientId
                        || t.CurrentClientId == clientId
                    )
                )
                .SelectMany(o => o.Tickets)
                .Where(t =>
                    t.EventSchedule.EventId == eventId
                    && t.OriginalOrder != null
                    && (
                        t.OriginalClientId == clientId
                        || t.CurrentClientId == clientId
                    )
                )
                .GroupBy(t => new
                {
                    EventId = t.EventSchedule.Event.Id,
                    OrderId = t.OriginalOrder!.Id
                })
                .Select(g => new
                {
                    OrderId = g.Key.OrderId,
                    EventId = g.Key.EventId,
                    EventKey = g.First().EventSchedule.ExternalEventKey,
                    LegacyPosterUrl = g.First().EventSchedule.Event.PosterImageUrl,
                    Folio = g.First().OriginalOrder!.Reference,
                    Name = g.First().EventSchedule.Event.Name,
                    Date = g.First().EventSchedule.Event.Schedules
                        .Min(s => s.StartDateTime),
                    Location = g.First().EventSchedule.Event.VenueMap.Name,
                    SubTotal = g.First().OriginalOrder!.SubTotal,
                    TotalFees = g.First().OriginalOrder!.TotalFees,
                    TotalTaxes = g.First().OriginalOrder!.TotalTaxes,
                    Total = g.First().OriginalOrder!.Total,
                    Seats = g
                        .GroupBy(x => x.SectionLabelSnapshot)
                        .Select(gTicket => new MyEventSeatDTO
                        {
                            Section = $"{gTicket.Key} x{gTicket.Count()}",
                            Seats = string.Join(", ", gTicket.Select(x => x.SeatLabelSnapshot))
                        })
                        .ToList(),
                    SelectedSeats = g.Select(seat => seat.EventSeat.ExternalSeatObjectKey).ToList(),
                    Currency = "MXN" // TODO: Add currency support for totals
                })
                .FirstOrDefaultAsync();

            var banner = DbContext.Set<Media>()
                .Include(m => m.BlobAsset)
                .AvailableBlobMedia()
                .Where(m =>
                    m.ReferenceId == eventId &&
                    m.ReferenceType == ClientSaleType.Event &&
                    m.MediaType == ClientMediaType.Banner
                )
                .OrderBy(m => m.Order)
                .FirstOrDefault();

            if (result == null)
            {
                return null;
            }

            var detail = new MyEventDetailDTO
            {
                OrderId = result.OrderId,
                EventId = result.EventId,
                EventKey = result.EventKey,
                EventImage = banner != null && banner.Url != null
                     ? banner.Url
                     : result.LegacyPosterUrl ?? string.Empty,
                Folio = result.Folio,
                Name = result.Name,
                Date = result.Date,
                Location = result.Location,
                SubTotal = result.SubTotal,
                TotalFees = result.TotalFees,
                TotalTaxes = result.TotalTaxes,
                Total = result.Total,
                Seats = result.Seats,
                SelectedSeats = result.SelectedSeats,
                Currency = result.Currency
            };

            return detail;
        }

        public async Task<PagedResponse<MyTicketDTO>> GetMyTicketsByOrderAsync(
            int page,
            int pageSize,
            long eventId,
            long orderId,
            long clientId)
        {
            var query = DbContext.Set<Order>()
                .Where(o => o.Id == orderId)
                .SelectMany(o => o.Tickets)
                .Where(t =>
                    t.EventSchedule.EventId == eventId
                    && (
                        t.OriginalClientId == clientId
                        || t.CurrentClientId == clientId
                    )
                )
                .OrderByDescending(t => t.EventSchedule.StartDateTime);

            int totalCount = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            var tickets = await query
                .GroupJoin(
                    DbContext.Set<Media>().AvailableBlobMedia().Where(x => x.ReferenceType == ClientSaleType.Event),
                    eventObject => eventObject.EventSchedule.EventId,
                    media => media.ReferenceId,
                    (t, m) => new
                    {
                        Ticket = t,
                        EventImages = m
                    }
                )
                .Skip(skip)
                .Take(pageSize)
                .Select(t => new
                {
                    Id = t.Ticket.Id,
                    OrderType = t.Ticket.OriginalOrder != null ? t.Ticket.OriginalOrder.OrderType : OrderType.Ticket,
                    Name = t.Ticket.EventSchedule.Event.Name,
                    Location = t.Ticket.EventSchedule.Event.VenueMap.Name,
                    StartDate = t.Ticket.EventSchedule.StartDateTime,
                    EventImageUrl = t.EventImages.Where(i => i.MediaType == ClientMediaType.Banner).OrderBy(i => i.Order).Select(i => i.BlobAsset.Url).FirstOrDefault(),
                    LegacyEventImageUrl = t.Ticket.EventSchedule.Event.PosterImageUrl,
                    Code = t.Ticket.TicketCode,
                    Section = t.Ticket.EventSection.BaseSection.Name,
                    Row = t.Ticket.EventSeat.BaseSeat.BaseRow.RowLabel,
                    Seat = t.Ticket.EventSeat.BaseSeat.SeatNumber,
                    CanShare = (
                        t.Ticket.OriginalClientId == clientId
                        && t.Ticket.CurrentClientId == clientId
                    ),
                    IsOwner = t.Ticket.OriginalClientId == clientId,
                    IsShared = (
                        t.Ticket.OriginalClientId != t.Ticket.CurrentClientId
                    )
                })
                .ToListAsync();

            var result = tickets.Select(t => new MyTicketDTO
            {
                Id = t.Id,
                OrderType = t.OrderType,
                Name = t.Name,
                Location = t.Location,
                StartDate = t.StartDate,
                EventImage = t.EventImageUrl != null
                    ? t.EventImageUrl
                    : t.LegacyEventImageUrl ?? string.Empty,
                Code = t.Code,
                Section = t.Section,
                Row = t.Row,
                Seat = t.Seat,
                CanShare = t.CanShare,
                IsOwner = t.IsOwner,
                IsShared = t.IsShared
            });

            return new PagedResponse<MyTicketDTO>
            {
                Items = result.ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<OrderDTO?> GetOrderAsync(long clientId, long orderId)
        {
            var orderData = await DbContext.Set<Order>()
                .Where(o => o.Id == orderId && o.ClientId == clientId)
                .Select(o => new
                {
                    o.Id,
                    o.Reference,
                    o.OrderType,
                    o.SubTotal,
                    o.TotalFees,
                    o.TotalTaxes,
                    o.Total,
                    Tickets = o.Tickets.Select(t => new
                    {
                        SectionName = t.EventSeat.EventSection.BaseSection.Name,
                        SeatNumber = t.EventSeat.BaseSeat.SeatNumber,
                        EventSeatId = t.EventSeatId,
                        SeatLabelSnapshot = t.SeatLabelSnapshot,
                        SeatPricePaid = t.PricePaid,
                        t.EventScheduleId,
                        EventSchedule = new
                        {
                            t.EventSchedule.Id,
                            t.EventSchedule.ExternalEventKey,
                            t.EventSchedule.StartDateTime,
                            Event = new
                            {
                                EventId = t.EventSchedule.Event.Id,
                                EventName = t.EventSchedule.Event.Name,
                                LegacyEventPosterUrl = t.EventSchedule.Event.PosterImageUrl,
                                VenueName = t.EventSchedule.Event.VenueMap.Name,

                                Season = t.EventSchedule.Event.Season == null ? null : new
                                {
                                    t.EventSchedule.Event.Season.Id,
                                    t.EventSchedule.Event.Season.Name,
                                    t.EventSchedule.Event.Season.ExternalSeasonKey,
                                    LegacySeasonPosterUrl = t.EventSchedule.Event.Season.PosterImageUrl,
                                    t.EventSchedule.Event.Season.StartDate
                                }
                            }
                        }
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (orderData == null || !orderData.Tickets.Any())
            {
                return null;
            }

            var seats = orderData.Tickets
                .Select(t => new
                {
                    t.SectionName,
                    t.SeatNumber
                })
                .Distinct()
                .GroupBy(x => x.SectionName)
                .Select(g => new MyEventSeatDTO
                {
                    Section = $"{g.Key} x{g.Count()}",
                    Seats = string.Join(", ", g.Select(x => x.SeatNumber).OrderBy(n => n))
                })
                .ToList();

            var seatsLabels = orderData.Tickets
                .Select(t => new SeatDTO
                {
                    Id = t.EventSeatId,
                    ExternalSeatObjectKey = t.SeatLabelSnapshot,
                    PriceOverride = t.SeatPricePaid
                })
                .GroupBy(s => new { s.ExternalSeatObjectKey, s.PriceOverride })
                .Select(g => g.First())
                .ToList();

            if (orderData.OrderType == OrderType.Ticket)
            {
                var first = orderData.Tickets.First();

                var eventBanner = DbContext.Set<Media>()
                .Include(m => m.BlobAsset)
                .AvailableBlobMedia()
                .Where(m =>
                    m.ReferenceId == first.EventSchedule.Event.EventId &&
                    m.ReferenceType == ClientSaleType.Event &&
                    m.MediaType == ClientMediaType.Banner
                )
                .OrderBy(m => m.Order)
                .FirstOrDefault();

                return new OrderDTO
                {
                    Id = orderData.Id,
                    Folio = orderData.Reference,
                    OrderType = orderData.OrderType,
                    SubTotal = orderData.SubTotal,
                    TotalFees = orderData.TotalFees,
                    TotalTaxes = orderData.TotalTaxes,
                    Total = orderData.Total,
                    Currency = "MXN",

                    ItemName = first.EventSchedule.Event.EventName,
                    ItemLocation = first.EventSchedule.Event.VenueName,
                    ItemKey = first.EventSchedule.ExternalEventKey,
                    ItemPosterImageUrl = eventBanner != null && eventBanner.Url != null
                        ? eventBanner.Url
                        : first.EventSchedule.Event.LegacyEventPosterUrl ?? string.Empty,
                    ItemStartDate = first.EventSchedule.StartDateTime,

                    ItemSeats = seats,
                    ItemSeatsLabels = seatsLabels
                };
            }
            else if (orderData.OrderType == OrderType.SeasonPass)
            {
                var seasons = orderData.Tickets
                    .Select(t => t.EventSchedule.Event.Season)
                    .Where(s => s != null)
                    .DistinctBy(s => s!.Id)
                    .ToList();

                if (seasons.Count != 1)
                {
                    throw new Exception("Invalid season order: múltiples seasons detectadas.");
                }

                var venues = orderData.Tickets
                    .Select(t => t.EventSchedule.Event.VenueName)
                    .Distinct()
                    .ToList();

                if (venues.Count != 1)
                {
                    throw new Exception("Invalid season order: múltiples venues detectados.");
                }

                var season = seasons.First()!;

                var seasonBanner = DbContext.Set<Media>()
                .Include(m => m.BlobAsset)
                .AvailableBlobMedia()
                .Where(m =>
                    m.ReferenceId == season.Id &&
                    m.ReferenceType == ClientSaleType.SeasonPass &&
                    m.MediaType == ClientMediaType.Banner
                )
                .OrderBy(m => m.Order)
                .FirstOrDefault();

                return new OrderDTO
                {
                    Id = orderData.Id,
                    Folio = orderData.Reference,
                    OrderType = orderData.OrderType,
                    SubTotal = orderData.SubTotal,
                    TotalFees = orderData.TotalFees,
                    TotalTaxes = orderData.TotalTaxes,
                    Total = orderData.Total,
                    Currency = "MXN",

                    ItemName = season.Name,
                    ItemLocation = venues.First(),
                    ItemKey = season.ExternalSeasonKey,
                    ItemPosterImageUrl = seasonBanner != null && seasonBanner.Url != null
                        ? seasonBanner.Url
                        : season.LegacySeasonPosterUrl ?? string.Empty,
                    ItemStartDate = season.StartDate,

                    ItemSeats = seats,
                    ItemSeatsLabels = seatsLabels
                };
            }
            else
            {
                throw new NotSupportedException($"OrderType {orderData.OrderType} no soportado.");
            }
        }

        public async Task<Order?> GetOrderWithItems(long orderId)
        {
            return await DbContext.Set<Order>()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }
    }
}
