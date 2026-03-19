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
                            EventId = t.EventSchedule.EventId,
                            EventName = t.EventSchedule.Event.Name,
                            EventImage = t.EventSchedule.Event.PosterImageUrl,
                            Location = t.EventSchedule.Event.VenueMap.Name,
                            TicketType = t.TicketType
                        })
                        .ToList()
                })
                .ToListAsync();

            var events = orders.Select(o =>
            {
                var currentSchedule = o.Tickets
                    .GroupBy(t => t.EventScheduleId)
                    .Select(g => g.First())
                    .OrderBy(t => t.StartDateTime < now)
                    .ThenByDescending(t => t.StartDateTime)
                    .First();

                return new MyEventDTO
                {
                    OrderId = o.Id,
                    EventId = currentSchedule.EventId,
                    EventImage = currentSchedule.EventImage,
                    Name = currentSchedule.EventName,
                    StartDate = currentSchedule.StartDateTime,
                    Location = currentSchedule.Location,
                    isSeasonPass = o.Tickets.Any(t => t.TicketType.ToUpper().Trim() == SEASONPASS),
                    isPastEvent = currentSchedule.StartDateTime < now
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
                    t.EventSchedule.EventId == eventId &&
                    t.OriginalOrder != null &&
                    t.CurrentClientId == clientId
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
                });

            return await query.SingleOrDefaultAsync();
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
                        t.EventSeat.EventSection.BaseSection.Name,
                        t.EventSeat.BaseSeat.SeatNumber,
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
                                EventCategories = t.EventSchedule.Event.Categories,
                                VenueName = t.EventSchedule.Event.VenueMap.Name
                            }
                        }
                    })
                    .ToList()
                })
                .FirstOrDefaultAsync();

            if (orderData == null)
                return null;

            var eventsGrouped = orderData.Tickets
                .GroupBy(t => t.EventScheduleId)
                .Select(g => new OrderEventDTO
                {
                    Id = g.First().EventSchedule.Event.EventId,
                    EventKey = g.First().EventSchedule.ExternalEventKey,
                    PosterImageUrl = g.First().EventSchedule.Event.PosterImageUrl,
                    Name = g.First().EventSchedule.Event.EventName,
                    StartDate = g.First().EventSchedule.StartDateTime,
                    Location = g.First().EventSchedule.Event.VenueName,
                    EventCategories = g.First().EventSchedule.Event.EventCategories
                        .Select(ec => new EventCategoryDTO
                        {
                            Id = ec.Id,
                            Name = ec.Name,
                            DisplayName = ec.DisplayName,
                        })
                        .ToList(),
                    Seats = g
                        .GroupBy(t => t.Name)
                        .Select(sg => new MyEventSeatDTO
                        {
                            Section = $"{sg.Key} x{sg.Count()}",
                            Seats = string.Join(", ", sg.Select(t => t.SeatNumber).OrderBy(n => n))
                        })
                        .ToList()
                })
                .ToList();

            return new OrderDTO
            {
                Id = orderData.Id,
                Folio = orderData.Reference,
                OrderType = orderData.OrderType,
                SubTotal = orderData.SubTotal,
                TotalFees = orderData.TotalFees,
                TotalTaxes = orderData.TotalTaxes,
                Total = orderData.Total,
                Currency = "MXN", // TODO: Add currency support for totals
                Events = eventsGrouped
            };
        }
    }
}
