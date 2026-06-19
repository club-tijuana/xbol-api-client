using Microsoft.Extensions.Logging;
using Odasoft.XBOL.Business.Messages;
using Odasoft.XBOL.Business.Services;
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

        private const string EVENT_ORDER_LOCALIZER_PREFIX = "ORD";
        private const string SEASON_ORDER_LOCALIZER_PREFIX = "ORD";

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
            var booked = false;

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

                ApplyVerifiedClientIdentity(command.Request.ClientContact, command.VerifiedClientId);

                command.Request.Localizer = await _sequenceTrackerService.GenerateLocalizerAsync(EVENT_ORDER_LOCALIZER_PREFIX);

                var tickets = await _ticketingClient.BookEventSeatsAsync(command.Request);
                booked = true;

                long orderId = await _orderService.CreateEventOrderAsync(command.Request);

                // PAY
                //if (!string.IsNullOrEmpty(command.Request.SessionId))
                //{
                //    var payResponse = await _ticketingClient.PayAsync(new PayRequest
                //    {
                //        OrderId = orderId,
                //        OrderRefId = command.Request.OrderRefId,
                //        SessionId = command.Request.SessionId,
                //        TransactionRefId = command.Request.TransactionRefId
                //    });
                //}
                // PAY

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
                if (booked && command.Request.Seats != null)
                {
                    await _ticketingClient.ReleaseSeatsActionAsync(new ReleaseSeatsByKeyRequest
                    {
                        EventKey = command.Request.EventKey,
                        Seats = command.Request.Seats.Select(s => s.SeatKey).ToArray()
                    });
                }

                _logger.LogError(ex, "Error creating event booking for event {EventKey}", command.Request.EventKey);
                throw;
            }
        }

        public async Task<BookingResult?> Handle(CreateSeasonBookingCommand command)
        {
            var booked = false;

            try
            {
                var season = await _seasonService.GetSeasonByExternalKeyAsync(command.Request.SeasonKey);

                if (season == null)
                {
                    _logger.LogWarning("Season with key {SeasonKey} not found", command.Request.SeasonKey);
                    return null;
                }

                ApplyVerifiedClientIdentity(command.Request.ClientContact, command.VerifiedClientId);

                //var canReserveSeason = await _bookingService.CanReserveSeasonAsync(season, command.VerifiedClientId);
                //if (!canReserveSeason.CanReserve)
                //{
                //    throw new Exception(canReserveSeason.Message);
                //}

                command.Request.Localizer = await _sequenceTrackerService.GenerateLocalizerAsync(SEASON_ORDER_LOCALIZER_PREFIX);

                //if (command.Request.ReferenceOrderId != null) // TODO: Execute this section if its renovation and the seats to be booked are Not For Sale
                //{
                //    if (!command.VerifiedClientId.HasValue)
                //    {
                //        throw new Exception("Season renewal requires a verified client identity.");
                //    }

                //    command.Request.HoldToken = "";

                //    SetForSaleRequest setForSaleRequest = new SetForSaleRequest
                //    {
                //        EventKey = command.Request.SeasonKey,
                //        ForSale = true,
                //        SeatKeys = command.Request.Seats.Select(s => s.SeatKey).ToList()
                //    };
                //    await _ticketingClient.SetForSaleAsync(setForSaleRequest);
                //}

                try
                {
                    var tickets = await _ticketingClient.BookSeasonSeatsAsync(command.Request);
                    booked = true;
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
                    //if (command.Request.ReferenceOrderId != null)
                    //{
                    //    SetForSaleRequest setForSaleRequest = new SetForSaleRequest
                    //    {
                    //        EventKey = command.Request.SeasonKey,
                    //        ForSale = false,
                    //        SeatKeys = command.Request.Seats.Select(s => s.SeatKey).ToList()
                    //    };
                    //    await _ticketingClient.SetForSaleAsync(setForSaleRequest);
                    //}
                    throw;
                }
            }
            catch (Exception ex)
            {
                if (booked && command.Request.SeasonKey != null && command.Request.Seats != null)
                {
                    await _ticketingClient.ReleaseSeatsActionAsync(new ReleaseSeatsByKeyRequest
                    {
                        EventKey = command.Request.SeasonKey,
                        Seats = command.Request.Seats.Select(s => s.SeatKey).ToArray()
                    });
                }

                _logger.LogError(ex, "Error creating season booking for season {SeasonKey}", command.Request.SeasonKey);
                throw;
            }
        }

        public static void ApplyVerifiedClientIdentity(ClientInfoRequest clientContact, long? verifiedClientId)
        {
            clientContact.Id = verifiedClientId;
        }
    }
}