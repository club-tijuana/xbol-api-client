using Odasoft.XBOL.Business.Messages;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.Commons.Requests;
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
        private readonly BookingService _bookingService;
        private readonly ClientService _clientService;

        private const string EVENT_ORDER_LOCALIZER_PREFIX = "ORD-E";
        private const string SEASON_ORDER_LOCALIZER_PREFIX = "ORD-S";

        public CreateEventBookingHandler(
            TicketingClient ticketingClient,
            EventScheduleService eventScheduleService,
            SequenceTrackerService sequenceTrackerService,
            SeasonService seasonService,
            OrderService orderService,
            BookingService bookingService,
            ClientService clientService
        )
        {
            _ticketingClient = ticketingClient;
            _eventScheduleService = eventScheduleService;
            _sequenceTrackerService = sequenceTrackerService;
            _seasonService = seasonService;
            _orderService = orderService;
            _bookingService = bookingService;
            _clientService = clientService;
        }

        public async Task<BookingResult?> Handle(CreateEventBookingCommand command)
        {
            try
            {
                var schedule = await _eventScheduleService.GetEventScheduleByExternalEventKeyAsync(command.Request.EventKey);

                if (schedule == null)
                {
                    Console.WriteLine($"Event with key {command.Request.EventKey} not found.");
                    return null;
                }

                var canReserveEvent = await _bookingService.CanReserveEventAsync(schedule);
                if (!canReserveEvent.CanReserve)
                {
                    throw new Exception(canReserveEvent.Message);
                }

                command.Request.Localizer = await _sequenceTrackerService.GenerateLocalizerAsync(EVENT_ORDER_LOCALIZER_PREFIX, schedule.EventId);

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
                var season = await _seasonService.GetSeasonByExternalKeyAsync(command.Request.SeasonKey);

                if (season == null)
                {
                    Console.WriteLine($"Season with key {command.Request.SeasonKey} not found.");
                    return null;
                }

                long? clientId = command.Request.ClientContact.Id;
                if (clientId == null)
                {
                    if (command.Request.ClientContact.Email == null || command.Request.ClientContact.PhoneNumber == null)
                    {
                        throw new Exception("Client information must be provided");
                    }

                    ClientContactRequest contact = new ClientContactRequest
                    {
                        Email = command.Request.ClientContact.Email,
                        Phone = command.Request.ClientContact.PhoneNumber,
                        PhoneCode = ""
                    };
                    var client = await _clientService.GetClientByContactAsync(contact);

                    if (client == null)
                    {
                        throw new Exception("Client information must be provided");
                    }

                    clientId = client.Id;
                }

                var canReserveSeason = await _bookingService.CanReserveSeasonAsync(season, clientId);
                if (!canReserveSeason.CanReserve)
                {
                    throw new Exception(canReserveSeason.Message);
                }

                command.Request.Localizer = await _sequenceTrackerService.GenerateLocalizerAsync(SEASON_ORDER_LOCALIZER_PREFIX, season.Id);

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
