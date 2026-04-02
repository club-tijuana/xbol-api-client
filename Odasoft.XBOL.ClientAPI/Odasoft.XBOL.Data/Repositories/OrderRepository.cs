using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Responses;
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
                    s.StartDate > now &&
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
                            EventImage = t.EventSchedule.Event.PosterImageUrl,
                            Location = t.EventSchedule.Event.VenueMap.Name,
                            TicketType = t.TicketType,
                            SeasonId = t.EventSchedule.Event.SeasonId,
                            SeasonName = t.EventSchedule.Event.Season != null ? t.EventSchedule.Event.Season.Name : ""
                        })
                        .ToList()
                })
                .ToListAsync();

            var clientSeasonIds = orders
                .SelectMany(o => o.Tickets)
                .Where(t => t.SeasonId.HasValue)
                .Select(t => t.SeasonId!.Value)
                .ToHashSet();

            var nextSeasonMap = await DbContext.Set<Season>()
                .Where(s => s.PreviousSeasonId != null)
                .ToDictionaryAsync(
                    s => s.PreviousSeasonId!.Value,
                    s => s.Id
                );

            var events = orders.Select(o =>
            {
                bool isSeason = o.Tickets.Any(t => t.TicketType.ToUpper().Trim() == SEASONPASS);

                var currentSchedule = o.Tickets
                    .GroupBy(t => t.EventScheduleId)
                    .Select(g => g.First())
                    .OrderBy(t => t.StartDateTime < now)
                    .ThenByDescending(t => t.StartDateTime)
                    .First();

                bool isPastEvent = false;

                if (isSeason)
                {
                    isPastEvent = o.Tickets.All(t => t.EndDateTime < now);
                }
                else
                {
                    isPastEvent = currentSchedule.StartDateTime < now;
                }

                bool hasNextSeason =
                    currentSchedule.SeasonId.HasValue &&
                    nextSeasonMap.TryGetValue(currentSchedule.SeasonId.Value, out var nextSeasonId) &&
                    clientSeasonIds.Contains(nextSeasonId);

                bool canRenovateSeasonPass =
                    isSeason &&
                    isPastEvent &&
                    currentSchedule.SeasonId.HasValue &&
                    renewableSeasonIds.Contains(currentSchedule.SeasonId.Value) &&
                    !hasNextSeason;

                return new MyEventDTO
                {
                    OrderId = o.Id,
                    EventId = currentSchedule.EventId,
                    EventImage = currentSchedule.EventImage,
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

        public async Task<MyEventDetailDTO?> GetMyEventDetailAsync(long clientId, long eventId)
        {
            var query = DbContext.Set<Order>()
                .Where(o => o.Tickets.Any(t => t.CurrentClientId == clientId))
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
                .Select(g => new MyEventDetailDTO
                {
                    OrderId = g.Key.OrderId,
                    EventId = g.Key.EventId,
                    EventKey = g.First().EventSchedule.ExternalEventKey,
                    EventImage = g.First().EventSchedule.Event.PosterImageUrl,
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

            return await query;
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
                .Skip(skip)
                .Take(pageSize)
                .Select(t => new MyTicketDTO
                {
                    Id = t.Id,
                    OrderType = t.OriginalOrder != null ? t.OriginalOrder.OrderType : OrderType.Ticket,
                    Name = t.EventSchedule.Event.Name,
                    Location = t.EventSchedule.Event.VenueMap.Name,
                    StartDate = t.EventSchedule.StartDateTime,
                    EventImage = t.EventSchedule.Event.PosterImageUrl,
                    Code = t.TicketCode,
                    Section = t.EventSection.BaseSection.Name,
                    Row = t.EventSeat.BaseSeat.BaseRow.RowLabel,
                    Seat = t.EventSeat.BaseSeat.SeatNumber,
                    CanShare = (
                        t.OriginalClientId == clientId
                        && t.CurrentClientId == clientId
                    ),
                    IsOwner = t.OriginalClientId == clientId,
                    IsShared = (
                        t.OriginalClientId != t.CurrentClientId
                    )
                })
                .ToListAsync();

            return new PagedResponse<MyTicketDTO>
            {
                Items = tickets,
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
                                t.EventSchedule.Event.PosterImageUrl,
                                VenueName = t.EventSchedule.Event.VenueMap.Name,

                                Season = t.EventSchedule.Event.Season == null ? null : new
                                {
                                    t.EventSchedule.Event.Season.Id,
                                    t.EventSchedule.Event.Season.Name,
                                    t.EventSchedule.Event.Season.ExternalSeasonKey,
                                    t.EventSchedule.Event.Season.PosterImageUrl,
                                    t.EventSchedule.Event.Season.StartDate
                                }
                            }
                        }
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (orderData == null || !orderData.Tickets.Any())
                return null;

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

            if (orderData.OrderType == OrderType.Ticket)
            {
                var first = orderData.Tickets.First();

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
                    ItemPosterImageUrl = first.EventSchedule.Event.PosterImageUrl,
                    ItemStartDate = first.EventSchedule.StartDateTime,

                    ItemSeats = seats
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
                    throw new Exception("Invalid season order: múltiples seasons detectadas.");

                var venues = orderData.Tickets
                    .Select(t => t.EventSchedule.Event.VenueName)
                    .Distinct()
                    .ToList();

                if (venues.Count != 1)
                    throw new Exception("Invalid season order: múltiples venues detectados.");

                var season = seasons.First()!;

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
                    ItemPosterImageUrl = season.PosterImageUrl,
                    ItemStartDate = season.StartDate,

                    ItemSeats = seats
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
