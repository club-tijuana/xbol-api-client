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
            long? idClient = null;
            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                idClient = token == "TEST-TOKEN" ? 1 : 2;
            }

            var result = await _seasonService.GetSeasonBannerAsync(idClient);
            return Ok(result);
        }
    }
}
