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
                    o.ClientId == idClient
                    && o.OrderType == orderType
                    && o.Tickets.Any()
                );

            int totalCount = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            List<MyEventDTO> events = await query
                .OrderBy(o => o.Id)
                .Skip(skip)
                .Take(pageSize)
                .Select(o => new
                {
                    Order = o,
                    Event = o.Tickets.Select(t => t.EventSchedule.Event).FirstOrDefault()
                })
                .Select(x => new MyEventDTO
                {
                    OrderId = x.Order.Id,
                    EventId = x.Event.Id,
                    EventImage = x.Event.PosterImageUrl,
                    Name = x.Event.Name,
                    StartDate = x.Event.Schedules
                        .OrderBy(s => s.StartDateTime)
                        .Select(s => s.StartDateTime)
                        .First(),
                    Location = x.Event.VenueMap.Name,
                    isSeasonPass = x.Order.Tickets.Any(t => t.TicketType.ToUpper().Trim() == SEASONPASS),
                    isPastEvent = x.Event.Schedules.All(s => s.StartDateTime < now)
                })
                .ToListAsync();

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
                .Where(o => o.ClientId == clientId)
                .SelectMany(o => o.Tickets)
                .Where(t =>
                    t.EventSchedule.EventId == eventId &&
                    t.OriginalOrder != null
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
            long orderId)
        {
            var query = DbContext.Set<Order>()
                .Where(o => o.Id == orderId)
                .SelectMany(o => o.Tickets)
                .Where(t => t.EventSchedule.EventId == eventId)
                .OrderByDescending(t => t.EventSchedule.StartDateTime);

            int totalCount = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            var tickets = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(t => new MyTicketDTO
                {
                    Id = t.Id,
                    Name = t.EventSchedule.Event.Name,
                    Location = t.EventSchedule.Event.VenueMap.Name,
                    StartDate = t.EventSchedule.StartDateTime,
                    EventImage = t.EventSchedule.Event.PosterImageUrl,
                    Code = t.TicketCode,
                    Section = t.EventSection.BaseSection.Name,
                    Row = t.EventSeat.BaseSeat.BaseRow.RowLabel,
                    Seat = t.EventSeat.BaseSeat.SeatNumber
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
