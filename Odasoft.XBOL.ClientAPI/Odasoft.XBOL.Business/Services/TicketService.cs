using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Business.Exceptions;
using Odasoft.XBOL.Commons.Requests;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.Business.Services
{
    public class TicketService
    {
        private readonly TicketRepository _ticketRepository;
        private readonly ClientService _clientService;

        public TicketService(TicketRepository ticketRepository, ClientService clientService)
        {
            _ticketRepository = ticketRepository;
            _clientService = clientService;
        }

        public async Task<MyTicketDTO?> ShareTicketAsync(ShareTicketRequest shareTicket, long requestClientId)
        {
            var client = await _clientService.GetClientByContactAsync(new ClientContactRequest
            {
                Email = shareTicket.Email,
                Phone = shareTicket.Phone,
                PhoneCode = shareTicket.PhoneCode
            });

            if (client == null)
            {
                throw new ClientNotFoundException();
            }

            var ticket = await _ticketRepository.GetTracked(
                filter: t => t.Id == shareTicket.TicketId,
                includedProperties: [
                    "EventSchedule",
                    "EventSchedule.Event",
                    "EventSchedule.Event.VenueMap",
                    "EventSection",
                    "EventSection.BaseSection",
                    "EventSeat",
                    "EventSeat.BaseSeat",
                    "EventSeat.BaseSeat.BaseRow",
                    "EventSchedule.Event.EventImages"
                ])
                .FirstOrDefaultAsync();

            if (ticket == null)
            {
                throw new TicketNotFoundException();
            }

            if (shareTicket.ApplyToEntireSeason)
            {
                var baseSeatId = ticket.EventSeat.BaseSeatId;
                var seasonId = ticket.EventSchedule.Event.SeasonId;

                var seasonTickets = await _ticketRepository.GetTracked(
                    filter: t =>
                        t.EventSeat.BaseSeatId == baseSeatId &&
                        t.EventSchedule.Event.SeasonId == seasonId &&
                        t.OriginalClientId == ticket.OriginalClientId
                ).ToListAsync();

                foreach (var t in seasonTickets)
                {
                    t.CurrentClientId = client.Id;
                }

                await _ticketRepository.UpdateRangeAsync(seasonTickets);
            }
            else
            {
                ticket.CurrentClientId = client.Id;
                await _ticketRepository.UpdateAsync(ticket);
            }

            var poster = ticket.EventSchedule.Event.EventImages.Where(i => i.ImageType == Commons.Enums.ImageType.VerticalPoster).FirstOrDefault();
            var legacyPoster = ticket.EventSchedule.Event.PosterImageUrl;

            return new MyTicketDTO
            {
                Id = ticket.Id,
                Name = ticket.EventSchedule.Event.Name,
                Location = ticket.EventSchedule.Event.VenueMap.Name,
                StartDate = ticket.EventSchedule.StartDateTime,
                EventImage = poster != null
                    ? $"data:{poster.ContentType};base64,{Convert.ToBase64String(poster.Content)}"
                    : legacyPoster ?? string.Empty,
                Code = ticket.TicketCode,
                Section = ticket.EventSection.BaseSection.Name,
                Row = ticket.EventSeat.BaseSeat.BaseRow.RowLabel,
                Seat = ticket.EventSeat.BaseSeat.SeatNumber,
                IsShared = true,
                CanShare = (
                    ticket.OriginalClientId == requestClientId
                    && ticket.CurrentClientId == requestClientId
                ),
                IsOwner = ticket.OriginalClientId == requestClientId
            };
        }

        public async Task<bool> UnshareTicketAsync(UnshareTicketRequest request, long requestClientId)
        {
            var ticket = await _ticketRepository.GetTracked(
                filter: t => t.Id == request.TicketId,
                includedProperties: [
                    "EventSchedule",
                    "EventSchedule.Event",
                    "EventSeat"
                ])
                .FirstOrDefaultAsync();

            if (ticket == null)
            {
                throw new TicketNotFoundException();
            }

            if (ticket.OriginalClientId != requestClientId)
            {
                throw new Exception("El ticket no pertenece al cliente");
            }

            if (request.ApplyToEntireSeason)
            {
                var baseSeatId = ticket.EventSeat.BaseSeatId;
                var seasonId = ticket.EventSchedule.Event.SeasonId;

                var seasonTickets = await _ticketRepository.GetTracked(
                    filter: t =>
                        t.EventSeat.BaseSeatId == baseSeatId &&
                        t.EventSchedule.Event.SeasonId == seasonId &&
                        t.OriginalClientId == requestClientId
                ).ToListAsync();

                foreach (var t in seasonTickets)
                {
                    if (t.CurrentClientId != t.OriginalClientId)
                    {
                        t.CurrentClientId = t.OriginalClientId;
                    }
                }

                await _ticketRepository.UpdateRangeAsync(seasonTickets);
            }
            else
            {
                if (ticket.CurrentClientId != ticket.OriginalClientId)
                {
                    ticket.CurrentClientId = ticket.OriginalClientId;
                    await _ticketRepository.UpdateAsync(ticket);
                }
            }

            return true;
        }
    }
}
