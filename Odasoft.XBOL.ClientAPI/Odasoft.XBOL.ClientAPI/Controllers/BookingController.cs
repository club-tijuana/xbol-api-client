using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Odasoft.XBOL.Business;
using Odasoft.XBOL.Business.Messages;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.ClientAPI.Services;
using Odasoft.XBOL.Commons.Requests.Filters;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.DTO.Results;
using Wolverine;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/bookings")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly BookingService _bookingService;
        private readonly IMessageBus _bus;
        private readonly ILogger<BookingController> _logger;
        private readonly IClientIdentityService _clientIdentityService;

        public BookingController(
            BookingService bookingService,
            IMessageBus bus,
            ILogger<BookingController> logger,
            IClientIdentityService clientIdentityService)
        {
            _bookingService = bookingService;
            _bus = bus;
            _logger = logger;
            _clientIdentityService = clientIdentityService;
        }

        [HttpGet("zones-by-schedule/{scheduleId}")]
        [EndpointName("GetZonesByEventIdAsync")]
        public async Task<ActionResult<List<ZoneDTO>>> GetZonesByEventIdAsync([FromRoute] long scheduleId)
        {
            var result = await _bookingService.GetZonesByEventIdAsync(scheduleId);

            return Ok(result);
        }

        [HttpGet("zones-by-season/{seasonId}")]
        [EndpointName("GetZonesBySeasonIdAsync")]
        public async Task<ActionResult<List<ZoneDTO>>> GetZonesBySeasonIdAsync([FromRoute] long seasonId)
        {
            //var result = await _bookingService.GetZonesBySeasonIdAsync(seasonId);
            var result = await _bookingService.GetZonesByBundleIdAsync(seasonId);

            return Ok(result);
        }

        [HttpPost("seats-availability")]
        [EndpointName("GetSeatAvailabilityAsync")]
        public async Task<ActionResult<SeatAvailabilityDTO>> GetSeatAvailabilityAsync([FromBody] ReservationFilters filters)
        {
            var result = await _bookingService.GetSeatAvailabilityAsync(filters);

            return Ok(result);
        }

        [HttpGet("event-by-schedule/{scheduleId}")]
        [EndpointName("GetEventItemByScheduleIdAsync")]
        public async Task<ActionResult<EventItemDTO>> GetEventItemByScheduleIdAsync(
            [FromRoute] long scheduleId,
            [FromQuery] bool includeMedia = false)
        {
            try
            {
                var result = await _bookingService.GetEventItemByScheduleIdAsync(scheduleId, includeMedia);

                if (result == null)
                {
                    return NotFound();
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get event item by schedule {ScheduleId}", scheduleId);
                return BadRequest(ex.Message);
            }
        }

        //[HttpGet("season-by-id/{seasonId}")]
        //[EndpointName("GetSeasonByScheduleIdAsync")]
        //public async Task<ActionResult<SeasonItemDTO>> GetSeasonByIdAsync(
        //    [FromRoute] long seasonId,
        //    [FromQuery] bool includeMedia = false)
        //{
        //    var client = await _clientIdentityService.TryResolveCurrentClientAsync(User);

        //    try
        //    {
        //        var result = await _bookingService.GetSeasonByIdAsync(seasonId, client?.Id, includeMedia);

        //        if (result != null)
        //        {
        //            return Ok(result);
        //        }
        //        else
        //        {
        //            return NotFound();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to get season by id {SeasonId}", seasonId);
        //        return BadRequest(ex.Message);
        //    }
        //}

        [HttpGet("bundle-by-id/{bundleId}")]
        [EndpointName("GetBundleSeasonByIdAsync")]
        public async Task<ActionResult<BundleItemDTO>> GetBundleSeasonByIdAsync(
            [FromRoute] long bundleId,
            [FromQuery] bool includeMedia = false)
        {
            var client = await _clientIdentityService.TryResolveCurrentClientAsync(User);

            try
            {
                var result = await _bookingService.GetBundleSeasonByIdAsync(bundleId, client?.Id, includeMedia);

                if (result != null)
                {
                    return Ok(result);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get season by id {BundleId}", bundleId);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Books seats for an event based on the provided booking request.
        /// </summary>
        /// <remarks>This method processes the booking asynchronously and returns appropriate HTTP status
        /// codes based on the outcome of the booking request.</remarks>
        /// <param name="request">The event booking request containing details such as event ID and number of seats to book. This parameter
        /// cannot be null.</param>
        /// <returns>A BookingResult object that contains the details of the booking operation, including confirmation of the
        /// booked seats.</returns>
        [HttpPost("event/book-seats")]
        [EndpointName("BookEventSeatsAsync")]
        [ProducesResponseType(typeof(BookingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ModelStateDictionary), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<BookingResult>> BookSeatsAsync([FromBody] EventBookingRequest request)
        {
            if (ModelState.IsValid == false)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var verifiedClientId = await TryResolveVerifiedClientIdAsync();
                var result = await _bus.InvokeAsync<BookingResult?>(
                    new CreateEventBookingCommand(request, verifiedClientId));
                if (result is null)
                {
                    return UnprocessableEntity("Booking failed. Please check the request details and try again.");
                }

                return Ok(result);
            }
            catch (ApiException ex)
            {
                _logger.LogWarning(ex, "Ticketing API error during event booking {Status}", ex.StatusCode);

                return ex.Response != null
                    ? StatusCode(ex.StatusCode, ex.Response)
                    : StatusCode(ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                // TODO: Replace temporary BadRequest error handling with proper exception handling:
                // - Return clearer, more descriptive error messages
                // - Map business errors to 422
                // - Map not found errors to 404
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Books seats for an event based on the provided booking request.
        /// </summary>
        /// <remarks>This method processes the booking asynchronously and returns appropriate HTTP status
        /// codes based on the outcome of the booking request.</remarks>
        /// <param name="request">The event booking request containing details such as event ID and number of seats to book. This parameter
        /// cannot be null.</param>
        /// <returns>A BookingResult object that contains the details of the booking operation, including confirmation of the
        /// booked seats.</returns>
        [HttpPost("season/book-season")]
        [EndpointName("BookSeasonSeatsAsync")]
        [ProducesResponseType(typeof(BookingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ModelStateDictionary), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<List<string>>> BookSeasonSeatsAsync([FromBody] SeasonBookingRequest request)
        {
            if (ModelState.IsValid == false)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var verifiedClientId = await TryResolveVerifiedClientIdAsync();
                var result = await _bus.InvokeAsync<BookingResult?>(
                    new CreateSeasonBookingCommand(request, verifiedClientId));
                if (result is null)
                {
                    return UnprocessableEntity("Booking failed. Please check the request details and try again.");
                }

                return Ok(result);
            }
            catch (ApiException ex)
            {
                _logger.LogWarning(ex, "Ticketing API error during season booking {Status}", ex.StatusCode);

                return ex.Response != null
                    ? StatusCode(ex.StatusCode, ex.Response)
                    : StatusCode(ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("season/renovate-season")]
        [Authorize]
        [EndpointName("RenovateSeasonSeatsAsync")]
        [ProducesResponseType(typeof(BookingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ModelStateDictionary), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<List<string>>> RenovateSeasonSeatsAsync([FromBody] SeasonBookingRequest request)
        {
            try
            {
                if (ModelState.IsValid == false)
                {
                    return BadRequest(ModelState);
                }

                if (request.ReferenceOrderId == null)
                {
                    return BadRequest("Renovation must contain a previous order to reference.");
                }

                var client = await _clientIdentityService.RequireCurrentClientAsync(User);
                request.ClientContact.Id = client.Id;

                var result = await _bus.InvokeAsync<BookingResult>(new CreateSeasonBookingCommand(request, client.Id));

                if (result is null)
                {
                    return UnprocessableEntity("Renovation failed. Please check the request details and try again.");
                }

                return Ok(result);
            }
            catch (ApiException ex)
            {
                _logger.LogWarning(ex, "Ticketing API error during season renovation {Status}", ex.StatusCode);

                if (ex.Response != null)
                {
                    return BadRequest(ex.Response);
                }
                else
                {
                    return BadRequest(ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to renovate season seats");
                return BadRequest(ex.Message);
            }
        }

        private async Task<long?> TryResolveVerifiedClientIdAsync()
        {
            var client = await _clientIdentityService.TryResolveCurrentClientAsync(User);
            return client?.Id;
        }
    }
}
