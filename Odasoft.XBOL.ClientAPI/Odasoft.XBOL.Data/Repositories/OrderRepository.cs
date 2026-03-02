using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class OrderRepository(XBOLDbContext dbContext) : BaseRepository<Order>(dbContext)
    {
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
                                EventCategory = t.EventSchedule.Event.Category,
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
                    EventCategory = g.First().EventSchedule.Event.EventCategory,
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
