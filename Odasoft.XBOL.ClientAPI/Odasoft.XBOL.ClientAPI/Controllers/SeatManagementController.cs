using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Odasoft.XBOL.Business;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.ClientAPI.Services;
using Odasoft.XBOL.DTO.Results;
using System.Globalization;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/seat-management")]
    [ApiController]
    public class SeatManagementController(
        SeatManagementService seatManagementService,
        ITicketingClient ticketingClient,
        IClientIdentityService clientIdentityService,
        IStringLocalizer<SharedResource>? localizer = null
    ) : ControllerBase
    {
        private static readonly IReadOnlyDictionary<string, string> FallbackMessages = new Dictionary<string, string>
        {
            ["BookingCreatedSuccessfully"] = "Booking created successfully",
            ["BookingFailed"] = "Booking failed. Please check the request details and try again."
        };

        /// <summary>
        /// Books the specified seats, optionally consuming a hold token,
        /// and creates the corresponding Order in the database.
        /// </summary>
        /// <param name="request">The seats with prices, optional hold token, and booking details.</param>
        /// <returns>A booking result containing the order ID, reference, and booked seat keys.</returns>
        [HttpPost("book")]
        [EndpointName("BookSeatsByIdAsync")]
        [ProducesResponseType(typeof(BookingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<BookingResult>> BookSeatsAsync([FromBody] BookSeatsBody request)
        {
            BookingResult? result = null;

            var verifiedClientId = await TryResolveVerifiedClientIdAsync();

            if (request.ClientContact == null)
            {
                request.ClientContact = new ClientInfoRequest();
                request.ClientContact.Id = verifiedClientId;
            }

            if (request.BundleId is long bundleId)
            {
                var booking = await ticketingClient.BookSeatsActionAsync(new BookSeatsActionRequest
                {
                    EventKey = string.Empty,
                    Seats = request.Seats,
                    HoldToken = request.HoldToken ?? "",
                    BundleId = bundleId,
                    EventScheduleId = null,
                    TicketType = request.TicketType,
                    ClientContact = request.ClientContact,
                    PaymentInfoRequest = request.PaymentInfoRequest,
                    ChangeInfoRequest = request.ChangeInfoRequest,
                    Localizer = request.Localizer,
                    ReferenceOrderId = request.ReferenceOrderId,
                    IsPaymentLink = request.IsPaymentLink,
                    PaymentLinkRequest = request.PaymentLinkRequest
                });

                result = new BookingResult
                {
                    BookingId = booking.OrderId,
                    Message = T("BookingCreatedSuccessfully"),
                    Tickets = booking.BookedSeatKeys ?? [],
                    TicketIds = booking.TicketIds ?? [],
                    BundlePassIds = booking.BundlePassIds ?? [],
                    ClientEmail = request.ClientContact.Email,
                    ClientPhone = request.ClientContact.PhoneNumber,
                    Localizer = booking.Reference ?? string.Empty
                };
            }
            else if (request.EventScheduleId is long eventScheduleId)
            {
                var externalKey = await seatManagementService.GetScheduleExternalKeyAsync(eventScheduleId);
                if (externalKey is null)
                {
                    return NotFound();
                }

                var booking = await ticketingClient.BookSeatsActionAsync(new BookSeatsActionRequest
                {
                    EventKey = externalKey,
                    Seats = request.Seats,
                    HoldToken = request.HoldToken ?? "",
                    BundleId = null,
                    EventScheduleId = eventScheduleId,
                    TicketType = request.TicketType,
                    ClientContact = request.ClientContact,
                    PaymentInfoRequest = request.PaymentInfoRequest,
                    ChangeInfoRequest = request.ChangeInfoRequest,
                    Localizer = request.Localizer,
                    ReferenceOrderId = request.ReferenceOrderId,
                    IsPaymentLink = request.IsPaymentLink,
                    PaymentLinkRequest = request.PaymentLinkRequest
                });

                result = new BookingResult
                {
                    BookingId = booking.OrderId,
                    //Reference = booking.Reference,
                    Message = T("BookingCreatedSuccessfully"),
                    Tickets = booking.BookedSeatKeys ?? [],
                    TicketIds = booking.TicketIds ?? [],
                    BundlePassIds = booking.BundlePassIds ?? [],
                    ClientEmail = request.ClientContact.Email,
                    ClientPhone = request.ClientContact.PhoneNumber,
                    Localizer = booking.Reference ?? string.Empty
                };
            }

            if (result is null)
            {
                return UnprocessableEntity(T("BookingFailed"));
            }

            return Ok(result);
        }

        private async Task<long?> TryResolveVerifiedClientIdAsync()
        {
            var client = await clientIdentityService.TryResolveCurrentClientAsync(User);
            return client?.Id;
        }

        private string T(string key, params object[] arguments)
        {
            if (localizer is not null)
            {
                return arguments.Length == 0
                    ? localizer[key].Value
                    : localizer[key, arguments].Value;
            }

            var template = FallbackMessages.TryGetValue(key, out var fallback)
                ? fallback
                : key;

            return arguments.Length == 0
                ? template
                : string.Format(CultureInfo.InvariantCulture, template, arguments);
        }
    }
}

public class BookSeatsBody
{
    public required List<BookingSeatRequest> Seats { get; set; }
    public string? HoldToken { get; set; }
    public long? BundleId { get; set; }
    public long? EventScheduleId { get; set; }
    public required ItemType TicketType { get; set; }
    public ClientInfoRequest? ClientContact { get; set; }
    public required PaymentInfoRequest PaymentInfoRequest { get; set; }
    public ChangeInfoRequest? ChangeInfoRequest { get; set; }
    public string? Localizer { get; set; }
    public long? ReferenceOrderId { get; set; }
    public bool IsPaymentLink { get; set; }
    public PaymentLinkRequest? PaymentLinkRequest { get; set; }
}
