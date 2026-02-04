using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Requests.Filters;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class ClientRepository(XBOLDbContext dbContext) : BaseRepository<Client>(dbContext)
    {
        public async Task<(List<MyEventTicketDTO> Items, int TotalCount)> GetMyEventTicketsAsync(TicketsFilters filters, long idClient)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var query = DbContext.Set<Models.Ticket>()
                .Where(t =>
                    t.CurrentClientId == idClient
                    && t.TicketType == (filters.TicketType != null ? filters.TicketType : t.TicketType)
                )
                .GroupBy(t => t.EventSchedule.EventId);

            int totalCount = await query.CountAsync();
            var skip = (filters.Page - 1) * filters.PageSize;

            List<MyEventTicketDTO> eventTickets = await query
                .OrderByDescending(g => g.Min(t => t.EventSchedule.StartDateTime))
                .Skip(skip)
                .Take(filters.PageSize)
                .Select(g => new MyEventTicketDTO
                {
                    Id = g.Key,
                    Name = g.First().EventSchedule.Event.Name,
                    Location = g.First().EventSchedule.Event.VenueMap.Name,
                    StartDate = g.First().EventSchedule.Event.Schedules
                        .OrderBy(s => s.StartDateTime)
                        .Select(s => s.StartDateTime)
                        .FirstOrDefault(),
                    isSeasonPass = g.Any(t => t.TicketType.ToUpper().Trim() == "SEASONPASS"),
                    isPastEvent = g.First().EventSchedule.Event.Schedules
                        .All(s => s.StartDateTime < now)
                })
                .ToListAsync();

            return (eventTickets, totalCount);
        }

        public async Task<(List<MyTicketDTO> Items, int TotalCount)> GetMyTicketsByEventAsync(TicketsFilters filters, long idClient)
        {
            var query = DbContext.Set<Models.Ticket>()
                .Where(t =>
                    t.CurrentClientId == idClient
                    && t.EventSchedule.EventId == filters.EventId
                )
                .OrderByDescending(t => t.EventSchedule.StartDateTime);

            int totalCount = await query.CountAsync();
            var skip = (filters.Page - 1) * filters.PageSize;

            List<MyTicketDTO> tickets = await query
                .Skip(skip)
                .Take(filters.PageSize)
                .Select(t => new MyTicketDTO
                {
                    Id = t.Id,
                    Name = t.EventSchedule.Event.Name,
                    Location = t.EventSchedule.Event.VenueMap.Name,
                    StartDate = t.EventSchedule.StartDateTime
                })
                .ToListAsync();

            return (tickets, totalCount);
        }
    }
}
