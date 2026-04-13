using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Odasoft.XBOL.Business;
using Odasoft.XBOL.Business.Messages;
using Odasoft.XBOL.Business.Services;
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

        public BookingController(BookingService bookingService, IMessageBus bus, ILogger<BookingController> logger)
        {
            _bookingService = bookingService;
            _bus = bus;
            _logger = logger;
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
            var result = await _bookingService.GetZonesBySeasonIdAsync(seasonId);

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
        public async Task<ActionResult<EventItemDTO>> GetEventItemByScheduleIdAsync([FromRoute] long scheduleId)
        {
            try
            {
                var result = await _bookingService.GetEventItemByScheduleIdAsync(scheduleId);

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

        [HttpGet("season-by-id/{seasonId}")]
        [EndpointName("GetSeasonByScheduleIdAsync")]
        public async Task<ActionResult<SeasonItemDTO>> GetSeasonByIdAsync([FromRoute] long seasonId)
        {
            // TODO: Remove temp token
            long? idClient = null;
            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                idClient = token == "TEST-TOKEN" ? 1 : 2;
            }

            try
            {
                var result = await _bookingService.GetSeasonByIdAsync(seasonId, idClient);

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
                _logger.LogError(ex, "Failed to get season by id {SeasonId}", seasonId);
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

            var result = await _bus.InvokeAsync<BookingResult?>(new CreateEventBookingCommand(request));

            if (result is null)
            {
                return UnprocessableEntity("Booking failed. Please check the request details and try again.");
            }

            return Ok(result);
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

            var result = await _bus.InvokeAsync<BookingResult?>(new CreateSeasonBookingCommand(request));

            if (result is null)
            {
                return UnprocessableEntity("Booking failed. Please check the request details and try again.");
            }

            return Ok(result);
        }

        [HttpPost("season/renovate-season")]
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

                var result = await _bus.InvokeAsync<BookingResult>(new CreateSeasonBookingCommand(request));

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
                    return BadRequest(ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to renovate season seats");
                return BadRequest(ex);
            }
        }
    }
}
