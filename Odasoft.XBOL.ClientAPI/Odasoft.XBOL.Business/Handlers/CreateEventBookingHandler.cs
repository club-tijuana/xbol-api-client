using Odasoft.XBOL.Business.Messages;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.DTO.Results;

namespace Odasoft.XBOL.Business.Handlers
{
    public class CreateEventBookingHandler
    {
        private readonly TicketingClient _ticketingClient;
        private readonly OrderService _orderService;

        public CreateEventBookingHandler(TicketingClient ticketingClient, OrderService orderService)
        {
            _ticketingClient = ticketingClient;
            _orderService = orderService;
        }

        public async Task<BookingResult> Handle(CreateEventBookingCommand command)
        {
            var orderId = await _orderService.CreateEventOrderAsync(command.Request);
            var tickets = await _ticketingClient.BookEventSeatsAsync(command.Request);

            return (new BookingResult { Message = "Booking created successfully", Tickets = tickets, OrderId = orderId });
        }

        public async Task<BookingResult> Handle(CreateSeasonBookingCommand command)
        {
            var orderId = await _orderService.CreateSeasonOrderAsync(command.Request);
            var tickets = await _ticketingClient.BookSeasonSeatsAsync(command.Request);

            return (new BookingResult { Message = "Booking created successfully", Tickets = tickets, OrderId = orderId });
        }
    }
}
