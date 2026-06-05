using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.DTO.Results;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/phone-region-codes")]
    [ApiController]
    public class PhoneNumbersController : ControllerBase
    {
        private readonly PhoneRegionCodesService _libPhoneNumberService;

        public PhoneNumbersController(PhoneRegionCodesService libPhoneNumberService)
        {
            _libPhoneNumberService = libPhoneNumberService;
        }

        /// <summary>
        /// Gets a list of phone region codes, including their corresponding dial codes and flag emojis.
        /// </summary>
        /// <returns>A list of objects containing the phone region code info.</returns>
        [HttpGet]
        [EndpointName("GetPhoneRegionCodesAsync")]
        [ProducesResponseType(typeof(List<PhoneRegionCodeResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PhoneRegionCodeResponse>>> GetPhoneRegionCodesAsync()
        {
            var countries = await _libPhoneNumberService.GetPhoneRegionCodesAsync();
            return Ok(countries);
        }
    }
}
