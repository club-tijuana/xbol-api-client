using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Responses;
using Odasoft.XBOL.Data.Projections;
using Odasoft.XBOL.Data.Queries;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class OrderRepository(XBOLDbContext dbContext) : BaseRepository<Order>(dbContext)
    {
        public async Task<PagedOrdersProjection> GetMyEventsAsync(
            int page,
            int pageSize,
            OrderType orderType,
            long idClient
        )
        {
            var query = DbContext.Set<Models.Order>()
                .Where(o =>
                    o.Tickets.Any()
                    && o.Tickets.Any(t =>
                        t.OriginalClientId == idClient ||
                        t.CurrentClientId == idClient
                    )
                    && o.OrderType == orderType
                    && o.Tickets.All(t => t.EventSchedule.Event.Status == EventStatus.Published)
                    && o.Tickets.All(t => t.EventSchedule.Event.DeletedAt == null)
                );

            int totalCount = await query.CountAsync();
            int skip = (page - 1) * pageSize;

            var orders = await query
                .OrderBy(o => o.Id)
                .Skip(skip)
                .Take(pageSize)
                .Select(o => new MyOrderProjection
                {
                    Id = o.Id,
                    Reference = o.Reference,
                    Tickets = o.Tickets
                        .Where(t =>
                            t.OriginalClientId == idClient ||
                            t.CurrentClientId == idClient
                        )
                        .Select(t => new MyOrderTicketProjection
                        {
                            EventScheduleId = t.EventScheduleId,
                            StartDateTime = t.EventSchedule.StartDateTime,
                            EndDateTime = t.EventSchedule.EndDateTime,

                            EventId = t.EventSchedule.EventId,
                            EventName = t.EventSchedule.Event.Name,
                            BannerUrl = DbContext.Set<Media>()
                                .Where(m =>
                                    m.ReferenceId == t.EventSchedule.Event.Id &&
                                    m.ReferenceType == ClientSaleType.Event &&
                                    m.MediaType == ClientMediaType.Banner
                                )
                                .OrderBy(m => m.Order)
                                .Select(m => m.Url)
                                .FirstOrDefault(),

                            LegacyPosterUrl = t.EventSchedule.Event.PosterImageUrl,
                            Location = t.EventSchedule.Event.VenueMap.Venue.Name,
                            TicketType = t.TicketType,
                            SeasonId = t.EventSchedule.Event.SeasonId,
                            SeasonName = t.EventSchedule.Event.Season != null
                                ? t.EventSchedule.Event.Season.Name
                                : ""
                        })
                        .ToList()
                })
                .ToListAsync();

            return new PagedOrdersProjection
            {
                Orders = orders,
                TotalCount = totalCount
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
                    && o.Tickets.All(t => t.EventSchedule.Event.Status == EventStatus.Published)
                    && o.Tickets.All(t => t.EventSchedule.Event.DeletedAt == null)
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
                    Location = g.First().EventSchedule.Event.VenueMap.Venue.Name,
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
            var order = await DbContext.Set<Order>()
                .Where(o => o.Id == orderId)
                .FirstOrDefaultAsync();

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
                    t.Ticket,
                    t.EventImages,
                    ItemReferenceId =
                        t.Ticket.SeasonPassEventTicket != null
                            ? (long?)t.Ticket.SeasonPassEventTicket.SeasonPassId
                            : t.Ticket.Id
                })
                .Select(t => new
                {
                    Id = t.Ticket.Id,
                    OrderReference = t.Ticket.OriginalOrder.Reference,
                    OrderType = t.Ticket.OriginalOrder != null ? t.Ticket.OriginalOrder.OrderType : OrderType.Ticket,
                    Name = t.Ticket.EventSchedule.Event.Name,
                    Location = t.Ticket.EventSchedule.Event.VenueMap.Venue.Name,
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
                    ),
                    IsCourtesy = t.Ticket.OriginalOrder.Items
                        .Where(oi => oi.ItemReferenceId == t.ItemReferenceId)
                        .Select(oi => (bool?)oi.IsCourtesy)
                        .FirstOrDefault() ?? false,
                    PricePaid = t.Ticket.PricePaid
                })
                .ToListAsync();

            var result = tickets.Select(t => new MyTicketDTO
            {
                Id = t.Id,
                OrderReference = t.OrderReference,
                OrderType = t.OrderType,
                Name = t.Name,
                StartDate = t.StartDate,
                Location = t.Location,
                EventImage = t.EventImageUrl != null
                    ? t.EventImageUrl
                    : t.LegacyEventImageUrl ?? string.Empty,
                Code = t.Code,
                Type = t.IsCourtesy ? TicketType.Courtesy : TicketType.Adult,
                PricePaid = t.PricePaid,
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

        public async Task<OrderDTO?> GetOrderAsync(long? clientId, long orderId, bool? isPaymentLink = false)
        {
            Dictionary<long, long?> ticketPriceListLookup;

            var orderInfo = await DbContext.Set<Order>()
                .Where(o => o.Id == orderId)
                .Select(o => new
                {
                    o.Id,
                    o.OrderType
                })
                .FirstOrDefaultAsync();

            if (orderInfo == null)
            {
                throw new Exception("Order not found");
            }

            if (orderInfo.OrderType == OrderType.Ticket)
            {
                ticketPriceListLookup = await DbContext.Set<OrderItem>()
                    .Where(oi => oi.OrderId == orderId)
                    .ToDictionaryAsync(
                        oi => oi.ItemReferenceId,
                        oi => oi.PriceListItemId
                    );
            }
            else
            {
                ticketPriceListLookup = await (
                    from oi in DbContext.Set<OrderItem>()
                    join spet in DbContext.Set<SeasonPassEventTicket>()
                        on oi.ItemReferenceId equals spet.SeasonPassId
                    where oi.OrderId == orderId
                    select new
                    {
                        spet.TicketId,
                        oi.PriceListItemId
                    })
                    .ToDictionaryAsync(
                        x => x.TicketId,
                        x => x.PriceListItemId
                    );
            }

            var orderData = await DbContext.Set<Order>()
                .Where(o => o.Id == orderId)
                .Select(o => new
                {
                    o.Id,
                    o.ClientId,
                    o.Reference,
                    o.OrderType,
                    o.SubTotal,
                    o.TotalFees,
                    o.TotalTaxes,
                    o.Discount,
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
                                VenueName = t.EventSchedule.Event.VenueMap.Venue.Name,

                                Season = t.EventSchedule.Event.Season == null ? null : new
                                {
                                    t.EventSchedule.Event.Season.Id,
                                    t.EventSchedule.Event.Season.Name,
                                    t.EventSchedule.Event.Season.ExternalSeasonKey,
                                    LegacySeasonPosterUrl = t.EventSchedule.Event.Season.PosterImageUrl,
                                    t.EventSchedule.Event.Season.StartDate
                                }
                            }
                        },
                        PriceListItemId = ticketPriceListLookup.GetValueOrDefault(t.Id)
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (orderData == null || !orderData.Tickets.Any())
            {
                return null;
            }

            if (clientId != null)
            {
                if (orderData.ClientId != clientId)
                {
                    throw new Exception("The order does not belong to the client.");
                }
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
                    PriceOverride = t.SeatPricePaid,
                    PriceListItemId = t.PriceListItemId
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
                    Folio = (clientId != null || isPaymentLink == true) ? orderData.Reference : "",
                    OrderType = orderData.OrderType,
                    SubTotal = (clientId != null || isPaymentLink == true) ? orderData.SubTotal : 0,
                    TotalFees = (clientId != null || isPaymentLink == true) ? orderData.TotalFees : 0,
                    TotalTaxes = (clientId != null || isPaymentLink == true) ? orderData.TotalTaxes : 0,
                    Discount = (clientId != null || isPaymentLink == true) ? orderData.Discount : 0,
                    Total = (clientId != null || isPaymentLink == true) ? orderData.Total : 0,
                    Currency = (clientId != null || isPaymentLink == true) ? "MXN" : "",

                    ItemName = first.EventSchedule.Event.EventName,
                    ItemLocation = first.EventSchedule.Event.VenueName,
                    ItemKey = first.EventSchedule.ExternalEventKey,
                    ItemPosterImageUrl = eventBanner != null && eventBanner.Url != null
                        ? eventBanner.Url
                        : first.EventSchedule.Event.LegacyEventPosterUrl ?? string.Empty,
                    ItemStartDate = first.EventSchedule.StartDateTime,

                    ItemSeats = (clientId != null || isPaymentLink == true) ? seats : new List<MyEventSeatDTO>(),
                    ItemSeatsLabels = (clientId != null || isPaymentLink == true) ? seatsLabels : new List<SeatDTO>()
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
                    Folio = (clientId != null || isPaymentLink == true) ? orderData.Reference : "",
                    OrderType = orderData.OrderType,
                    SubTotal = (clientId != null || isPaymentLink == true) ? orderData.SubTotal : 0,
                    TotalFees = (clientId != null || isPaymentLink == true) ? orderData.TotalFees : 0,
                    TotalTaxes = (clientId != null || isPaymentLink == true) ? orderData.TotalTaxes : 0,
                    Discount = (clientId != null || isPaymentLink == true) ? orderData.Discount : 0,
                    Total = (clientId != null || isPaymentLink == true) ? orderData.Total : 0,
                    Currency = (clientId != null || isPaymentLink == true) ? "MXN" : "",

                    ItemName = season.Name,
                    ItemLocation = venues.First(),
                    ItemKey = season.ExternalSeasonKey,
                    ItemPosterImageUrl = seasonBanner != null && seasonBanner.Url != null
                        ? seasonBanner.Url
                        : season.LegacySeasonPosterUrl ?? string.Empty,
                    ItemStartDate = season.StartDate,

                    ItemSeats = (clientId != null || isPaymentLink == true) ? seats : new List<MyEventSeatDTO>(),
                    ItemSeatsLabels = (clientId != null || isPaymentLink == true) ? seatsLabels : new List<SeatDTO>()
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
