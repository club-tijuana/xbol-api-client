using Microsoft.AspNetCore.Mvc;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.DTO;

namespace Odasoft.XBOL.ClientAPI.Controllers
{
    [Route("api/seasons")]
    [ApiController]
    public class SeasonController : ControllerBase
    {
        private readonly SeasonService _seasonService;

        public SeasonController(SeasonService seasonService)
        {
            _seasonService = seasonService;
        }

        [HttpGet]
        [EndpointName("GetSeasonBannerAsync")]
        public async Task<ActionResult<SeasonItemDTO?>> GetSeasonBannerAsync()
        {
            var result = await _seasonService.GetSeasonBannerAsync();
            return Ok(result);
        }
    }
}
