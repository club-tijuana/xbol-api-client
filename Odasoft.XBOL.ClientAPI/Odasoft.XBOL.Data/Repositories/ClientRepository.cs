using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Requests.Filters;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class ClientRepository(XBOLDbContext dbContext) : BaseRepository<Client>(dbContext)
    {
        public async Task<(List<MyEventDTO> Items, int TotalCount)> GetMyEventsAsync(TicketsFilters filters, long idClient)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var query = DbContext.Set<Models.Order>()
                .Where(o =>
                    o.ClientId == idClient
                    && o.OrderType == filters.OrderType
                    && o.Tickets.Any()
                );

            int totalCount = await query.CountAsync();
            var skip = (filters.Page - 1) * filters.PageSize;

            List<MyEventDTO> events = await query
                .OrderBy(o => o.Id)
                .Skip(skip)
                .Take(filters.PageSize)
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
                    isSeasonPass = x.Order.Tickets.Any(t => t.TicketType.ToUpper().Trim() == "SEASONPASS"),
                    isPastEvent = x.Event.Schedules.All(s => s.StartDateTime < now)
                })
                .ToListAsync();

            return (events, totalCount);
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
                    Currency = "MXN"
                });

            return await query.SingleOrDefaultAsync();
        }

        public async Task<(List<MyTicketDTO> Items, int TotalCount)> GetMyTicketsByOrderAsync(TicketsFilters filters)
        {
            var query = DbContext.Set<Order>()
                .Where(o => o.Id == filters.OrderId)
                .SelectMany(o => o.Tickets)
                .OrderByDescending(t => t.EventSchedule.StartDateTime);

            int totalCount = await query.CountAsync();
            var skip = (filters.Page - 1) * filters.PageSize;

            var tickets = await query
                .Skip(skip)
                .Take(filters.PageSize)
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
                    Seat = t.EventSeat.BaseSeat.SeatNumber,
                    QR = "https://dev.zorbek.software/XBOL/images/QR/QR.png"
                })
                .ToListAsync();

            return (tickets, totalCount);
        }
    }
}
