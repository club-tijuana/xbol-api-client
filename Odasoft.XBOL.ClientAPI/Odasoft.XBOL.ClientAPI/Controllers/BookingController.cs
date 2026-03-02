using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.Commons.Requests.Filters;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/bookings")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly BookingService _bookingService;

        public BookingController(BookingService bookingService)
        {
            _bookingService = bookingService;
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
    }
}
