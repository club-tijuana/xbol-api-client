using Microsoft.Extensions.Logging;
using Odasoft.XBOL.Business.Messages;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.Commons.Requests;
using Odasoft.XBOL.DTO.Results;

namespace Odasoft.XBOL.Business.Handlers
{
    public class CreateEventBookingHandler
    {
        private readonly ITicketingClient _ticketingClient;
        private readonly EventScheduleService _eventScheduleService;
        private readonly SequenceTrackerService _sequenceTrackerService;
        private readonly SeasonService _seasonService;
        private readonly OrderService _orderService;
        private readonly BookingService _bookingService;
        private readonly ClientService _clientService;
        private readonly ILogger<CreateEventBookingHandler> _logger;

        private const string EVENT_ORDER_LOCALIZER_PREFIX = "ORD-E";
        private const string SEASON_ORDER_LOCALIZER_PREFIX = "ORD-S";

        public CreateEventBookingHandler(
            ITicketingClient ticketingClient,
            EventScheduleService eventScheduleService,
            SequenceTrackerService sequenceTrackerService,
            SeasonService seasonService,
            OrderService orderService,
            BookingService bookingService,
            ClientService clientService,
            ILogger<CreateEventBookingHandler> logger
        )
        {
            _ticketingClient = ticketingClient;
            _eventScheduleService = eventScheduleService;
            _sequenceTrackerService = sequenceTrackerService;
            _seasonService = seasonService;
            _orderService = orderService;
            _bookingService = bookingService;
            _clientService = clientService;
            _logger = logger;
        }

        public async Task<BookingResult?> Handle(CreateEventBookingCommand command)
        {
            try
            {
                var schedule = await _eventScheduleService.GetEventScheduleByExternalEventKeyAsync(command.Request.EventKey);

                if (schedule == null)
                {
                    _logger.LogWarning("Event with key {EventKey} not found", command.Request.EventKey);
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
                _logger.LogError(ex, "Error creating event booking for event {EventKey}", command.Request.EventKey);
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
                    _logger.LogWarning("Season with key {SeasonKey} not found", command.Request.SeasonKey);
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

                if (command.Request.ReferenceOrderId != null) // TODO: Execute this section if its renovation and the seats to be booked are Not For Sale
                {
                    command.Request.HoldToken = "";

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
                    if (command.Request.ReferenceOrderId != null)
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
                _logger.LogError(ex, "Error creating season booking for season {SeasonKey}", command.Request.SeasonKey);
                return null;
            }
        }
    }
}
