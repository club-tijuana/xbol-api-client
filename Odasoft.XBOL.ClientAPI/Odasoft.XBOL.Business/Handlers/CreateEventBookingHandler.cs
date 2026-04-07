using Odasoft.XBOL.Business.Messages;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.DTO.Results;

namespace Odasoft.XBOL.Business.Handlers
{
    public class CreateEventBookingHandler
    {
        private readonly TicketingClient _ticketingClient;
        private readonly EventScheduleService _eventScheduleService;
        private readonly SequenceTrackerService _sequenceTrackerService;
        private readonly SeasonService _seasonService;
        private readonly OrderService _orderService;

        private const string EVENT_ORDER_LOCALIZER_PREFIX = "ORD-E";
        private const string SEASON_ORDER_LOCALIZER_PREFIX = "ORD-S";

        public CreateEventBookingHandler(
            TicketingClient ticketingClient,
            EventScheduleService eventScheduleService,
            SequenceTrackerService sequenceTrackerService,
            SeasonService seasonService,
            OrderService orderService
        )
        {
            _ticketingClient = ticketingClient;
            _eventScheduleService = eventScheduleService;
            _sequenceTrackerService = sequenceTrackerService;
            _seasonService = seasonService;
            _orderService = orderService;
        }

        public async Task<BookingResult?> Handle(CreateEventBookingCommand command)
        {
            try
            {
                long? eventId = await _eventScheduleService.GetEventIdByExternalEventKeyAsync(command.Request.EventKey);

                if (eventId == null)
                {
                    Console.WriteLine($"Event with key {command.Request.EventKey} not found.");
                    return null;
                }

                command.Request.Localizer = await _sequenceTrackerService.GenerateLocalizerAsync(EVENT_ORDER_LOCALIZER_PREFIX, eventId.Value);

                var tickets = await _ticketingClient.BookEventSeatsAsync(command.Request);
                long orderId = await _orderService.CreateEventOrderAsync(command.Request);

                return new BookingResult
                {
                    Message = "Booking created successfully",
                    Tickets = tickets,
                    Localizer = command.Request.Localizer,
                    ClientEmail = command.Request.ClientContact.Email,
                    ClientPhone = command.Request.ClientContact.PhoneNumber,
                    OrderId = orderId
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating event booking: {ex.Message}");
                return null;
            }
        }

        public async Task<BookingResult?> Handle(CreateSeasonBookingCommand command)
        {
            try
            {
                long? seasonId = await _seasonService.GetSeasonIdByExternalKeyAsync(command.Request.SeasonKey);

                if (seasonId == null)
                {
                    Console.WriteLine($"Season with key {command.Request.SeasonKey} not found.");
                    return null;
                }

                command.Request.Localizer = await _sequenceTrackerService.GenerateLocalizerAsync(SEASON_ORDER_LOCALIZER_PREFIX, seasonId.Value);

                if (command.Request.RefereceOrderId != null) // TODO: Execute this section if its renovation and the seats to be booked are Not For Sale
                {
                    command.Request.HoldToken = null;

                    SetForSaleRequest setForSaleRequest = new SetForSaleRequest
                    {
                        EventKey = command.Request.SeasonKey,
                        ForSale = true,
                        SeatKeys = command.Request.Seats.Select(s => s.Key).ToList()
                    };
                    await _ticketingClient.SetForSaleAsync(setForSaleRequest);
                }

                try
                {
                    var tickets = await _ticketingClient.BookSeasonSeatsAsync(command.Request);
                    long orderId = await _orderService.CreateSeasonOrderAsync(command.Request);

                    return new BookingResult
                    {
                        Message = "Booking created successfully",
                        Tickets = tickets,
                        Localizer = command.Request.Localizer,
                        ClientEmail = command.Request.ClientContact.Email,
                        ClientPhone = command.Request.ClientContact.PhoneNumber,
                        OrderId = orderId
                    };
                }
                catch
                {
                    if (command.Request.RefereceOrderId != null)
                    {
                        SetForSaleRequest setForSaleRequest = new SetForSaleRequest
                        {
                            EventKey = command.Request.SeasonKey,
                            ForSale = false,
                            SeatKeys = command.Request.Seats.Select(s => s.Key).ToList()
                        };
                        await _ticketingClient.SetForSaleAsync(setForSaleRequest);
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating season booking: {ex.Message}");
                return null;
            }
        }
    }
}
