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

        public BookingController(BookingService bookingService, IMessageBus bus)
        {
            _bookingService = bookingService;
            _bus = bus;
        }

        [HttpGet("zones-by-schedule/{scheduleId}")]
        [EndpointName("GetZonesByEventIdAsync")]
        public async Task<ActionResult<List<ZoneDTO>>> GetZonesByEventIdAsync([FromRoute] long scheduleId)
        {
            var result = await _bookingService.GetZonesByEventIdAsync(scheduleId);

            return Ok(result);
        }

        [HttpPost("seats-availability")]
        [EndpointName("GetSeatAvailabilityAsync")]
        public async Task<ActionResult<List<SectionDTO>>> GetSeatAvailabilityAsync([FromBody] ReservationFilters filters)
        {
            var result = await _bookingService.GetSeatAvailabilityAsync(filters);

            return Ok(result);
        }

        [HttpGet("event-by-schedule/{scheduleId}")]
        [EndpointName("GetEventItemByScheduleIdAsync")]
        public async Task<ActionResult<EventItemDTO>> GetEventItemByScheduleIdAsync([FromRoute] long scheduleId)
        {
            var result = await _bookingService.GetEventItemByScheduleIdAsync(scheduleId);

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

            var result = await _bus.InvokeAsync<BookingResult>(new CreateEventBookingCommand(request));

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

            var result = await _bus.InvokeAsync<BookingResult>(new CreateSeasonBookingCommand(request));

            if (result is null)
            {
                return UnprocessableEntity("Booking failed. Please check the request details and try again.");
            }

            return Ok(result);
        }
    }
}
